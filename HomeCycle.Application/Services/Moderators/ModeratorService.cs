using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.DTOs.Responses.Moderators;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Domain.Entities;
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
        private readonly IBusinessServiceAreaRepository _businessServiceAreaRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IUserRepository _userRepository; 
        private readonly IEmailService _emailService;       
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModeratorService> _logger;

        public ModeratorService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IBusinessServiceAreaRepository businessServiceAreaRepository,
            IBankAccountRepository bankAccountRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<ModeratorService> logger)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _businessServiceAreaRepository = businessServiceAreaRepository;
            _bankAccountRepository = bankAccountRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> ReviewBusinessProfileAsync(
            Guid moderatorId,
            ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // 1. Kiểm tra sự tồn tại của hồ sơ cần phê duyệt
            var profile = await _businessProfileRepository.GetByIdAsync(request.BusinessProfileId, cancellationToken);
            if (profile == null)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Không tìm thấy hồ sơ đăng ký doanh nghiệp yêu cầu phê duyệt."));
            }

            // 2. Guard Clause: Chỉ cho phép duyệt khi hồ sơ đang ở trạng thái Pending (0)
            if (profile.Status != (int)BusinessProfileStatus.Pending)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest(
                    "Hồ sơ này đã được duyệt hoặc từ chối trước đó, không còn ở trạng thái chờ duyệt."));
            }

            // 3. Validation rule: Bắt buộc nhập lý do nếu Từ chối
            if (!request.IsApproved && string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Vui lòng nhập lý do từ chối hồ sơ doanh nghiệp."));
            }

            // 4. Lấy thông tin User gốc để phục vụ lấy Email gửi thông báo
            var user = await _userRepository.GetByIdAsync(profile.UserId, cancellationToken);
            if (user == null)
            {
                return Result<string>.Fail(ValidationErrors.InvalidRequest("Tài khoản liên kết với hồ sơ doanh nghiệp này không tồn tại trên hệ thống."));
            }

            // 5. THỰC THI GHI CƠ SỞ DỮ LIỆU TRONG TRANSACTION
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                profile.Status = request.IsApproved 
                    ? (int)BusinessProfileStatus.Approved 
                    : (int)BusinessProfileStatus.Rejected;

                profile.VerifiedBy = moderatorId;
                profile.VerifiedAt = now;
                profile.RejectReason = request.IsApproved ? null : request.RejectReason?.Trim();
                profile.UpdatedAt = now;

                _businessProfileRepository.Update(profile);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi nghiêm trọng khi Moderator {ModeratorId} duyệt hồ sơ BusinessProfile {ProfileId}", 
                    moderatorId, request.BusinessProfileId);
                throw;
            }

            // 6. GỬI EMAIL THÔNG BÁO (Ngoài Transaction - Resilient Fire-and-Forget)
            try
            {
                string businessNameDisplay = !string.IsNullOrWhiteSpace(profile.BusinessName) 
                    ? profile.BusinessName 
                    : user.Username;

                if (request.IsApproved)
                {
                    await _emailService.SendBusinessApprovalEmailAsync(user.Email, businessNameDisplay);
                }
                else
                {
                    // Adapter: Chuyển RejectReason duy nhất thành List<string> nếu IEmailService vẫn yêu cầu List
                    var rejectReasonsList = new List<string> { profile.RejectReason! };
                    await _emailService.SendBusinessRejectionEmailAsync(user.Email, businessNameDisplay, rejectReasonsList);
                }
            }
            catch (Exception emailEx)
            {
                // DB đã commit an toàn, chỉ log lỗi email chứ không throw hỏng response của client
                _logger.LogError(emailEx, "Database đã commit thành công nhưng dịch vụ gửi Email thất bại cho User: {Email}", user.Email);
            }

            return Result<string>.Success(request.IsApproved
                ? "Hồ sơ doanh nghiệp đã được phê duyệt chính thức. Email chúc mừng kích hoạt quyền kinh doanh đã gửi."
                : "Hồ sơ doanh nghiệp đã bị từ chối. Thư điện tử thông báo chi tiết lý do đã được gửi về cho User.");
        }

        public async Task<Result<BusinessRegistrationDetailDto>> GetBusinessProfileDetailForModeratorAsync(
            Guid businessProfileId,
            CancellationToken cancellationToken = default)
        {
            // 1. Fetch thông tin Profile gốc từ DB
            var profile = await _businessProfileRepository.GetByIdAsync(businessProfileId, cancellationToken);

            if (profile == null)
            {
                return Result<BusinessRegistrationDetailDto>.Fail(
                    ValidationErrors.InvalidRequest("Không tìm thấy hồ sơ đăng ký doanh nghiệp yêu cầu."));
            }

            // 2. Query song song: Documents, Service Areas và Bank Account
            // (Lưu ý: Bổ sung repository lấy thông tin tài khoản ngân hàng theo UserId hoặc BusinessProfileId)
            var documentsTask = _businessDocumentRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var serviceAreasTask = _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var bankAccountTask = _bankAccountRepository.GetByUserIdAsync(profile.UserId, cancellationToken);
            // ^ Tham chiếu đúng method/repository lấy Bank Account trong dự án của em

            await Task.WhenAll(documentsTask, serviceAreasTask, bankAccountTask);

            var documents = await documentsTask;
            var serviceAreas = await serviceAreasTask;
            var bankAccount = await bankAccountTask;

            // 3. Mapping chính xác sang BusinessRegistrationDetailDto
            var detailDto = new BusinessRegistrationDetailDto
            {
                BusinessProfileId = profile.BusinessProfileId,
                BusinessName = profile.BusinessName,
                FullName = profile.FullName,
                BusinessDescription = profile.BusinessDescription,
                TaxCode = profile.TaxCode,
                BusinessAddress = profile.BusinessAddress,
                Ward = profile.Ward,
                City = profile.City,
                IdentityNumber = profile.IdentityNumber,
                OperatingScope = profile.OperatingScope,
                BusinessModel = profile.BusinessModel,
                Status = profile.Status,
                RejectReason = profile.RejectReason,

                // Thông tin ngân hàng (Lấy từ bankAccount object, handle null an toàn)
                BankCode = bankAccount?.BankCode ?? string.Empty,
                BankName = bankAccount?.BankName ?? string.Empty,
                AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
                AccountName = bankAccount?.AccountName ?? string.Empty,

                // Map danh sách tài liệu
                Documents = documents.Select(doc => new BusinessRegistrationDocumentDto
                {
                    BusinessDocumentId = doc.BusinessDocumentId,
                    DocumentType = doc.DocumentType,
                    DocumentUrl = doc.DocumentUrl
                }).ToList(),

                // Map danh sách khu vực hoạt động
                ServiceAreas = serviceAreas.Select(sa => new BusinessRegistrationServiceAreaDto
                {
                    City = sa.City,
                    District = sa.District,
                    Ward = sa.Ward
                }).ToList()
            };

            return Result<BusinessRegistrationDetailDto>.Success(detailDto);
        }

        public async Task<Result<List<PendingBusinessProfileSummaryDto>>> GetPendingBusinessProfilesAsync(
            string? keyword,
            CancellationToken cancellationToken = default)
        {
            var profiles = await _businessProfileRepository.GetPendingProfilesAsync(keyword, cancellationToken);

            var response = profiles.Select(p => new PendingBusinessProfileSummaryDto
            {
                BusinessProfileId = p.BusinessProfileId,
                BusinessName = !string.IsNullOrWhiteSpace(p.BusinessName)
                    ? p.BusinessName
                    : (!string.IsNullOrWhiteSpace(p.FullName) ? p.FullName : "Chưa cập nhật tên"),
                CreatedAt = p.CreatedAt
            }).ToList();

            return Result<List<PendingBusinessProfileSummaryDto>>.Success(response);
        }
    }
}
