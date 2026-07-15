using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Moderators
{
    public class ModeratorService : IModeratorService
    {
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IBusinessDocumentRepository _businessDocumentRepository;
        private readonly IUserRepository _userRepository; 
        private readonly IEmailService _emailService;       
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModeratorService> _logger;

        public ModeratorService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<ModeratorService> logger)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> ReviewBusinessProfileAsync(
            Guid moderatorId,
            Guid profileId,
            ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // 1. Kiểm tra sự tồn tại của hồ sơ cần phê duyệt
            var profile = await _businessProfileRepository.GetByUserIdAsync(profileId, cancellationToken);
            if (profile == null)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Không tìm thấy hồ sơ đăng ký doanh nghiệp yêu cầu phê duyệt."));
            }

            if (profile.Status == (int)BusinessProfileStatus.Approved && request.IsApproved)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Hồ sơ doanh nghiệp này đã được phê duyệt chính thức trước đó."));
            }

            // 2. Truy vấn lấy thông tin User gốc để phục vụ lấy địa chỉ Email gửi thư thông báo công khai
            var user = await _userRepository.GetByIdAsync(profile.UserId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Tài khoản liên kết với hồ sơ doanh nghiệp này không tồn tại trên hệ thống."));
            }

            // 3. Tải toàn bộ tài liệu pháp lý đi kèm của hồ sơ
            var existingDocs = await _businessDocumentRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

            // Khởi tạo danh sách lưu vết lý do từ chối phục vụ gửi mail ngoại tuyến
            var emailRejectReasons = new List<string>();


            await _unitOfWork.BeginTransactionAsync();
            try
            {

                foreach (var reviewDto in request.DocumentReviews)
                {
                    var targetDoc = existingDocs.FirstOrDefault(x => x.BusinessDocumentId == reviewDto.DocumentId);
                    if (targetDoc == null)
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest($"Không tìm thấy tài liệu có ID {reviewDto.DocumentId} thuộc hồ sơ này."));
                    }

                    if (!reviewDto.IsApproved && string.IsNullOrWhiteSpace(reviewDto.RejectReason))
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest($"Tài liệu loại [{targetDoc.DocumentType}] bị từ chối duyệt nhưng Moderator chưa điền lý do lỗi cụ thể."));
                    }

                    targetDoc.Status = reviewDto.IsApproved ? (int)DocumentStatus.Approved : (int)DocumentStatus.Rejected;
                    targetDoc.VerifiedBy = moderatorId;
                    targetDoc.VerifiedAt = now;
                    targetDoc.RejectReason = reviewDto.IsApproved ? null : reviewDto.RejectReason!.Trim();
                    targetDoc.UpdatedAt = now;

                    if (!reviewDto.IsApproved)
                    {
                        emailRejectReasons.Add($"- Tài liệu [{targetDoc.DocumentType}]: {reviewDto.RejectReason!.Trim()}");
                    }
                }

                _businessDocumentRepository.UpdateRange(existingDocs);

                if (request.IsApproved)
                {
                    var hasRejectedDoc = existingDocs.Any(x => x.Status == (int)DocumentStatus.Rejected);
                    if (hasRejectedDoc)
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest(
                            "Không thể phê duyệt hồ sơ tổng thể khi hệ thống phát hiện có tài liệu pháp lý đi kèm bị đánh dấu từ chối (Rejected)."));
                    }

                    profile.Status = (int)BusinessProfileStatus.Approved; 
                }
                else
                {
                    var hasRejectedDoc = existingDocs.Any(x => x.Status == (int)DocumentStatus.Rejected);
                    if (!hasRejectedDoc)
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest(
                            "Bạn quyết định TỪ CHỐI hồ sơ tổng thể, vui lòng chọn từ chối ít nhất một tài liệu pháp lý đi kèm và nhập lý do cụ thể."));
                    }

                    profile.Status = (int)BusinessProfileStatus.Rejected;
                }

                profile.CurrentModeratorId = moderatorId;
                profile.UpdatedAt = now;

                _businessProfileRepository.Update(profile);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Critical error occurred while Moderator reviewed Business Profile for UserId: {UserId}", profile.UserId);
                throw;
            }


            try
            {
                string businessNameDisplay = profile.BusinessName ?? user.Username;

                if (request.IsApproved)
                {
    
                    await _emailService.SendBusinessApprovalEmailAsync(user.Email, businessNameDisplay);
                }
                else
                {
  
                    await _emailService.SendBusinessRejectionEmailAsync(user.Email, businessNameDisplay, emailRejectReasons);
                }
            }
            catch (Exception emailEx)
            {
                // Ghi nhận lỗi hệ thống gửi mail vào log, tuyệt đối không quăng Exception ở đây làm vỡ kết quả trả về của Client
                // Vì database đã commit thành công, hồ sơ của người dùng thực tế đã thay đổi trạng thái an toàn
                _logger.LogError(emailEx, "Database committed successfully but Background Email Notification service failed for user: {Email}", user.Email);
            }

            return Result<string>.Success(request.IsApproved
                ? "Hồ sơ doanh nghiệp đã được phê duyệt chính thức. Email chúc mừng kích hoạt quyền kinh doanh đã gửi."
                : "Hồ sơ doanh nghiệp đã bị từ chối. Thư điện tử chi tiết các đầu mục lỗi đã được gửi về cho User.");
        }
    }
}
