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
        private readonly IBusinessProcurementPreferenceRepository _preferenceRepository;
        private readonly IBusinessProductTypeRepository _businessProductTypeRepository;
        private readonly IBusinessServiceAreaRepository _businessServiceAreaRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusinessProfileService> _logger;

        public BusinessProfileService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IBusinessProcurementPreferenceRepository preferenceRepository,
            IBusinessProductTypeRepository businessProductTypeRepository,
            IBusinessServiceAreaRepository businessServiceAreaRepository,
            IBankAccountRepository bankAccountRepository,
            IUnitOfWork unitOfWork,
            ILogger<BusinessProfileService> logger)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _preferenceRepository = preferenceRepository;
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
                        Status = (int)BusinessProfileStatus.Pending, 
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
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();
                await _businessDocumentRepository.AddRangeAsync(businessDocs, cancellationToken);

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

        public async Task<Result<BusinessRegistrationDetailDto>> GetRegistrationDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
            {
                return Result<BusinessRegistrationDetailDto>.Fail(
                    new Error("BusinessProfile.NotFound", "Business profile could not be found for this user account."));
            }


            var bankAccount = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);

            var documents = await _businessDocumentRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var serviceAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);


            var registrationDetail = new BusinessRegistrationDetailDto
            {
                BusinessProfileId = profile.BusinessProfileId,
                BusinessName = profile.BusinessName ?? string.Empty,
                FullName = profile.FullName,
                BusinessDescription = profile.BusinessDescription,
                TaxCode = profile.TaxCode ?? string.Empty,
                BusinessAddress = profile.BusinessAddress ?? string.Empty,
                Ward = profile.Ward ?? string.Empty,
                City = profile.City ?? string.Empty,
                IdentityNumber = profile.IdentityNumber ?? string.Empty,
                OperatingScope = profile.OperatingScope,
                BusinessModel = profile.BusinessModel, 
                Status = profile.Status,
                RejectReason = profile.RejectReason,

                BankCode = bankAccount?.BankCode ?? string.Empty,
                BankName = bankAccount?.BankName ?? string.Empty,
                AccountNumber = bankAccount?.AccountNumber ?? string.Empty,
                AccountName = bankAccount?.AccountName ?? string.Empty,

                Documents = documents.Select(doc => new BusinessRegistrationDocumentDto
                {
                    BusinessDocumentId = doc.BusinessDocumentId,
                    DocumentType = doc.DocumentType,
                    DocumentUrl = doc.DocumentUrl ?? string.Empty,
                }).ToList(),



                ServiceAreas = serviceAreas.Select(sa => new BusinessRegistrationServiceAreaDto
                {
                    City = sa.City ?? string.Empty,
                    District = sa.District ?? string.Empty,
                    Ward = sa.Ward ?? string.Empty
                }).ToList()
            };

            return Result<BusinessRegistrationDetailDto>.Success(registrationDetail);
        }


        public async Task<Result> SaveProcurementPreferenceAsync(Guid userId, SubmitBusinessSurveyRequest request, CancellationToken cancellationToken)
        {
            var businessProfile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken); 
            if (businessProfile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp tương ứng."));

            Guid businessProfileId = businessProfile.BusinessProfileId;

            await _unitOfWork.BeginTransactionAsync(); 
            try
            {
                // 1. Lưu / Cập nhật Khảo sát nhu cầu (Preference)
                var domainPreference = await _preferenceRepository.GetByBusinessProfileIdAsync(businessProfileId, cancellationToken); 

                if (domainPreference == null)
                {
                    var newPreference = new business_procurement_preference
                    {
                        PreferenceId = Guid.NewGuid(), 
                        BusinessProfileId = businessProfileId, 
                        TargetCities = request.TargetCities, 
                        AcceptableDamageLevels = request.AcceptableDamageLevels, 
                        AcceptableFunctionalityStatuses = request.AcceptableFunctionalityStatuses, 
                        ProcurementScales = request.ProcurementScales, 
                        CreatedAt = DateTime.UtcNow 
                    };
                    await _preferenceRepository.AddAsync(newPreference, cancellationToken); 
                }
                else
                {
                    domainPreference.TargetCities = request.TargetCities; 
                    domainPreference.AcceptableDamageLevels = request.AcceptableDamageLevels; 
                    domainPreference.AcceptableFunctionalityStatuses = request.AcceptableFunctionalityStatuses; 
                    domainPreference.ProcurementScales = request.ProcurementScales; 
                    domainPreference.UpdatedAt = DateTime.UtcNow; 

                    _preferenceRepository.Update(domainPreference); 
                }

                // 2. Dọn sạch danh mục loại sản phẩm cũ & Chèn danh mục mới
                await _businessProductTypeRepository.DeleteAllByProfileIdAsync(businessProfileId, cancellationToken); 

                if (request.ProductTypeIds != null && request.ProductTypeIds.Any())
                {
                    var newProductTypes = new List<business_product_type>();
                    int priority = 1;
                    foreach (var typeId in request.ProductTypeIds) 
                    {
                        newProductTypes.Add(new business_product_type
                        {
                            BusinessProductTypeId = Guid.NewGuid(), 
                            BusinessProfileId = businessProfileId, 
                            ProductTypeId = typeId, 
                            Priority = priority++, 
                            CreatedAt = DateTime.UtcNow 
                        });
                    }
                    await _businessProductTypeRepository.AddRangeAsync(newProductTypes, cancellationToken); 
                }

                // 3. Commit DB
                await _unitOfWork.SaveChangesAsync(cancellationToken); 
                await _unitOfWork.CommitTransactionAsync(); 

                return Result.Success(); 
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(); 
                _logger.LogError(ex, "Lỗi xảy ra khi lưu khảo sát nhu cầu thu mua cho UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<Result<BusinessSurveyDetailResponse>> GetProcurementPreferenceAsync(Guid userId, CancellationToken cancellationToken)
        {
            var businessProfile = await _businessProfileRepository.GetByUserIdAsync(userId);
            if (businessProfile == null)
                return Result<BusinessSurveyDetailResponse>.Fail(
                    new Error("Survey.NotFound", "The business procurement preference survey has not been completed."));

            var preference = await _preferenceRepository.GetByBusinessProfileIdAsync(businessProfile.BusinessProfileId, cancellationToken);
            if (preference == null)
                return Result<BusinessSurveyDetailResponse>.Fail(
                    new Error("BusinessProfile.NotFound", "Business profile could not be found."));

            var productTypesEntities = await _businessProductTypeRepository.GetByProfileIdAsync(businessProfile.BusinessProfileId);
            var productTypeIds = productTypesEntities.Select(pt => pt.ProductTypeId).ToList(); // Ép kiểu tường minh về List<Guid>

            var response = new BusinessSurveyDetailResponse
            {
                TargetCities = preference.TargetCities,
                AcceptableDamageLevels = preference.AcceptableDamageLevels,
                AcceptableFunctionalityStatuses = preference.AcceptableFunctionalityStatuses,
                ProcurementScales = preference.ProcurementScales,
                ProductTypeIds = productTypeIds
            };

            return Result<BusinessSurveyDetailResponse>.Success(response);
        }

        public async Task<Result<BusinessOnboardingStatus>> GetOnboardingStatusAsync(Guid userId, CancellationToken cancellationToken)
        {
            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            if (profile == null)
                return Result<BusinessOnboardingStatus>.Success(BusinessOnboardingStatus.MissingProfile);

            if (profile.Status == (int)BusinessProfileStatus.Pending)
                return Result<BusinessOnboardingStatus>.Success(BusinessOnboardingStatus.PendingApproval);

            if (profile.Status == (int)BusinessProfileStatus.Rejected)
                return Result<BusinessOnboardingStatus>.Success(BusinessOnboardingStatus.Rejected);

            if (profile.Status == (int)BusinessProfileStatus.Approved)
            {
                bool hasSurvey = await _preferenceRepository.ExistsByBusinessProfileIdAsync(profile.BusinessProfileId, cancellationToken);
                if (!hasSurvey)
                    return Result<BusinessOnboardingStatus>.Success(BusinessOnboardingStatus.SurveyPending);
            }

            return Result<BusinessOnboardingStatus>.Success(BusinessOnboardingStatus.Completed);
        }

       

     
    }
}

