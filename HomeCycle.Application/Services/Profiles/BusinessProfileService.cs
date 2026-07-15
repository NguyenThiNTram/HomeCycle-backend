using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Profiles
{
    public class BusinessProfileService : IBusinessProfileService
    {
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IBusinessDocumentRepository _businessDocumentRepository;
        private readonly IBusinessProductTypeRepository _businessProductTypeRepository;
        private readonly IBusinessServiceAreaRepository _businessServiceAreaRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusinessProfileService> _logger;

        public BusinessProfileService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IBusinessProductTypeRepository businessProductTypeRepository,
            IBusinessServiceAreaRepository businessServiceAreaRepository,
            IBankAccountRepository bankAccountRepository,
            IUnitOfWork unitOfWork,
            ILogger<BusinessProfileService> logger)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _businessProductTypeRepository = businessProductTypeRepository;
            _businessServiceAreaRepository = businessServiceAreaRepository;
            _bankAccountRepository = bankAccountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> SubmitBusinessProfileAsync(
            Guid userId,
            SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // 1. TRUY VẤN KIỂM TRA HỒ SƠ DOANH NGHIỆP HIỆN TẠI
            var existingProfile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);


            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Guid targetProfileId;

                if (existingProfile == null)
                {
                    // KỊCH BẢN A: NỘP HỒ SƠ LẦN ĐẦU (ADD NEW)
                    targetProfileId = Guid.NewGuid();

                    var newProfile = new business_profile
                    {
                        BusinessProfileId = targetProfileId,
                        UserId = userId,
                        BusinessName = request.BusinessName.Trim(),
                        FullName = request.FullName?.Trim(),
                        BusinessDescription = request.BusinessDescription?.Trim(),
                        TaxCode = request.TaxCode.Trim(),
                        BusinessAddress = request.BusinessAddress.Trim(),
                        Ward = request.Ward.Trim(),
                        City = request.City.Trim(),
                        IdentityNumber = request.IdentityNumber.Trim(),
                        OperatingScope = request.OperatingScope?.Trim(),
                        BusinessModel = request.BusinessModel,
                        Status = (int)BusinessProfileStatus.Pending, // 0 - Pending
                        CurrentModeratorId = null,
                        ReputationScore = 100,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _businessProfileRepository.AddAsync(newProfile, cancellationToken);

                    // Khởi tạo tài khoản liên kết ngân hàng lần đầu
                    var bankAccount = new bank_account
                    {
                        UserBankId = Guid.NewGuid(),
                        UserId = userId,
                        BankCode = request.BankCode.Trim(),
                        BankName = request.BankName.Trim(),
                        AccountNumber = request.AccountNumber.Trim(),
                        AccountName = request.AccountName.Trim().ToUpper(),
                        VerifyStatus = (int)BankVerifyStatus.Unverified, // 0
                        CreatedAt = now
                    };
                    await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);
                }
                else
                {

                    // KỊCH BẢN B: NỘP LẠI HỒ SƠ (RESUBMIT)
                    // Chốt chặn bảo mật: Chỉ cho phép sửa khi trạng thái hiện tại là Rejected (3)
                    if (existingProfile.Status != (int)BusinessProfileStatus.Rejected)
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest(
                            "Your application is either pending approval or has already been approved. It cannot be edited at this time."));
                    }

                    targetProfileId = existingProfile.BusinessProfileId;

                    // 1. Cập nhật thông tin cơ bản & Reset trạng thái tổng về Pending (0)
                    existingProfile.BusinessName = request.BusinessName.Trim();
                    existingProfile.FullName = request.FullName?.Trim();
                    existingProfile.BusinessDescription = request.BusinessDescription?.Trim();
                    existingProfile.TaxCode = request.TaxCode.Trim();
                    existingProfile.BusinessAddress = request.BusinessAddress.Trim();
                    existingProfile.Ward = request.Ward.Trim();
                    existingProfile.City = request.City.Trim();
                    existingProfile.IdentityNumber = request.IdentityNumber.Trim();
                    existingProfile.OperatingScope = request.OperatingScope?.Trim();
                    existingProfile.BusinessModel = request.BusinessModel;
                    existingProfile.Status = (int)BusinessProfileStatus.Pending; 
                    existingProfile.UpdatedAt = now;

                    _businessProfileRepository.Update(existingProfile);

                    // 2. DỌN SẠCH DỮ LIỆU LIÊN KẾT CŨ TRÁNH RÁC DATABASE
                    await _businessDocumentRepository.DeleteAllByProfileIdAsync(targetProfileId, cancellationToken);
                    await _businessProductTypeRepository.DeleteAllByProfileIdAsync(targetProfileId, cancellationToken);
                    await _businessServiceAreaRepository.DeleteAllByProfileIdAsync(targetProfileId, cancellationToken);

                    // 3. Cập nhật tài khoản ngân hàng liên kết & Reset trạng thái BankVerify về Unverified
                    var existingBank = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);
                    if (existingBank != null)
                    {
                        existingBank.BankCode = request.BankCode.Trim();
                        existingBank.BankName = request.BankName.Trim();
                        existingBank.AccountNumber = request.AccountNumber.Trim();
                        existingBank.AccountName = request.AccountName.Trim().ToUpper();
                        existingBank.VerifyStatus = (int)BankVerifyStatus.Unverified;

                        _bankAccountRepository.Update(existingBank);
                    }
                    else
                    {
                        var bankAccount = new bank_account
                        {
                            UserBankId = Guid.NewGuid(),
                            UserId = userId,
                            BankCode = request.BankCode.Trim(),
                            BankName = request.BankName.Trim(),
                            AccountNumber = request.AccountNumber.Trim(),
                            AccountName = request.AccountName.Trim().ToUpper(),
                            VerifyStatus = (int)BankVerifyStatus.Unverified,
                            CreatedAt = now
                        };
                        await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);
                    }
                }


                // 1. Bulk Insert Documents (Duyệt LINQ cực nhanh trên RAM, chạy 1 hàm AddRangeAsync tối ưu I/O)
                var businessDocs = request.Documents.Select(docDto => new business_document
                {
                    BusinessDocumentId = Guid.NewGuid(),
                    BusinessProfileId = targetProfileId,
                    DocumentType = docDto.DocumentType, 
                    DocumentUrl = docDto.DocumentUrl.Trim(),
                    Status = (int)DocumentStatus.Pending,
                    VerifiedBy = null,
                    VerifiedAt = null, 
                    CreatedAt = now,
                    UpdatedAt = now,
                    RejectReason = null
                }).ToList();
                await _businessDocumentRepository.AddRangeAsync(businessDocs, cancellationToken);

                // 2. Bulk Insert Product Types
                var bizProductTypes = request.ProductTypeIds.Select(productTypeId => new business_product_type
                {
                    BusinessProductTypeId = Guid.NewGuid(),
                    BusinessProfileId = targetProfileId,
                    ProductTypeId = productTypeId,
                    Priority = 0,
                    CreatedAt = now
                }).ToList();
                await _businessProductTypeRepository.AddRangeAsync(bizProductTypes, cancellationToken);

                // 3. Bulk Insert Service Areas (Chỉ nạp nếu là Doanh nghiệp Enterprise và có thông tin đăng ký)
                if (request.BusinessModel == (int)BusinessModel.Enterprise && request.ServiceAreas != null && request.ServiceAreas.Any())
                {
                    var serviceAreas = request.ServiceAreas.Select(areaDto => new business_service_area
                    {
                        BusinessServiceAreaId = Guid.NewGuid(),
                        BusinessProfileId = targetProfileId,
                        City = areaDto.City.Trim(),
                        District = areaDto.District.Trim(),
                        Ward = areaDto.Ward.Trim(),
                        Priority = 0,
                        CreatedAt = now
                    }).ToList();
                    await _businessServiceAreaRepository.AddRangeAsync(serviceAreas, cancellationToken);
                }

                // 4. THỰC THI GHI XUỐNG POSTGRESQL & COMMIT TRANSACTION
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<string>.Success(existingProfile == null
                    ? "The business registration application has been submitted successfully and is awaiting approval."
                    : "The resubmitted application has been updated successfully and is awaiting re-approval.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Critical error occurred while executing SubmitBusinessProfile transaction for UserId: {UserId}", userId);
                throw;
            }
        }

        //public async Task<Result<BusinessRegistrationDetailDto>> GetRegistrationDetailAsync(
        //    Guid userId,
        //    CancellationToken cancellationToken = default)
        //{
        //    // A. Lấy thông tin Business Profile thô của User
        //    var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        //    if (profile == null)
        //    {
        //        return Result<BusinessRegistrationDetailDto>.Fail(
        //            ValidationErrors.NotFound("Hồ sơ doanh nghiệp của tài khoản này hiện chưa được khởi tạo."));
        //    }

        //    // B. Lấy thông tin tài khoản ngân hàng liên kết phục vụ Napas/PayOS
        //    var bankAccount = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);

        //    // C. Truy vấn tất cả thông tin phụ ở các bảng con thông qua Profile ID
        //    var documents = await _businessDocumentRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
        //    var productTypes = await _businessProductTypeRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
        //    var serviceAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

        //    // D. Dựng DTO chi tiết gửi về cho Frontend
        //    var registrationDetail = new BusinessRegistrationDetailDto
        //    {
        //        BusinessProfileId = profile.BusinessProfileId,
        //        BusinessName = profile.BusinessName ?? string.Empty,
        //        FullName = profile.FullName,
        //        BusinessDescription = profile.BusinessDescription,
        //        TaxCode = profile.TaxCode ?? string.Empty,
        //        BusinessAddress = profile.BusinessAddress ?? string.Empty,
        //        Ward = profile.Ward ?? string.Empty,
        //        City = profile.City ?? string.Empty,
        //        IdentityNumber = profile.IdentityNumber ?? string.Empty,
        //        OperatingScope = profile.OperatingScope,
        //        BusinessModel = profile.BusinessModel, // Đồng bộ kiểu dữ liệu string? với Domain
        //        Status = profile.Status,

        //        // Gán thông tin tài khoản ngân hàng
        //        BankCode = bankAccount?.BankCode ?? string.Empty,
        //        BankName = bankAccount?.BankName ?? string.Empty,
        //        AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
        //        AccountName = bankAccount?.AccountName ?? string.Empty,

        //        // Gán danh sách tài liệu pháp lý kèm trạng thái duyệt và Reject Reason của từng file
        //        Documents = documents.Select(doc => new BusinessRegistrationDocumentDto
        //        {
        //            BusinessDocumentId = doc.BusinessDocumentId,
        //            DocumentType = doc.DocumentType, // Đồng bộ kiểu dữ liệu string? với Domain
        //            DocumentUrl = doc.DocumentUrl ?? string.Empty,
        //            Status = doc.Status ?? 0,
        //            RejectReason = doc.RejectReason // Trả về lý do từ chối cụ thể để Frontend hiển thị inline
        //        }).ToList(),

        //        // Gán danh mục ngành hàng liên kết
        //        ProductTypeIds = productTypes.Select(pt => pt.ProductTypeId).ToList(),

        //        // Gán danh sách kho bãi (Dành cho Enterprise)
        //        ServiceAreas = serviceAreas.Select(sa => new BusinessRegistrationServiceAreaDto
        //        {
        //            City = sa.City ?? string.Empty,
        //            District = sa.District ?? string.Empty,
        //            Ward = sa.Ward ?? string.Empty
        //        }).ToList()
        //    };

        //    return Result<BusinessRegistrationDetailDto>.Success(registrationDetail);
        //}

        public async Task<Result<BusinessProfileStatus?>> GetProfileStatusAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
        {
            // SELECT thực thể Profile thô từ Database
            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            // Nếu chưa tồn tại bất kỳ bản ghi hồ sơ nào dưới DB -> Trả thẳng null
            if (profile == null)
            {
                return Result<BusinessProfileStatus?>.Success(null);
            }

            // Nếu đã tồn tại, Cast trực tiếp trạng thái int sang Domain Enum và trả về thẳng
            return Result<BusinessProfileStatus?>.Success((BusinessProfileStatus)profile.Status);
        }
        
    }
}
