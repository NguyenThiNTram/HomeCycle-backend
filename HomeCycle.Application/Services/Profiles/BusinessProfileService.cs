using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
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
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BusinessProfileService> _logger;
        private readonly IValidator<SubmitBusinessProfileRequest> _profileValidator;
        private readonly IValidator<SubmitBusinessSurveyRequest> _surveyValidator;
        private readonly IValidator<UpdateUsernameRequest> _updateUsernameValidator;
        private readonly IValidator<UpdatePhoneNumberRequest> _updatePhoneValidator;
        private readonly IValidator<UpdateAvatarRequest> _updateAvatarValidator;
        private readonly IValidator<UpdateBankAccountRequest> _updateBankValidator;
        private readonly IValidator<UpdateApprovedBusinessProfileRequest> _updateBusinessProfileValidator;
        private readonly IValidator<UpdateBusinessDocumentsRequest> _updateDocumentsValidator;
        private readonly IValidator<UpdateBusinessServiceAreasRequest> _updateServiceAreasValidator;

        public BusinessProfileService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IBusinessProcurementPreferenceRepository preferenceRepository,
            IBusinessProductTypeRepository businessProductTypeRepository,
            IBusinessServiceAreaRepository businessServiceAreaRepository,
            IBankAccountRepository bankAccountRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<BusinessProfileService> logger,
            IValidator<SubmitBusinessProfileRequest> profileValidator,
            IValidator<SubmitBusinessSurveyRequest> surveyValidator,
            IValidator<UpdateUsernameRequest> updateUsernameValidator,
            IValidator<UpdatePhoneNumberRequest> updatePhoneValidator,
            IValidator<UpdateAvatarRequest> updateAvatarValidator,
            IValidator<UpdateBankAccountRequest> updateBankValidator,
            IValidator<UpdateApprovedBusinessProfileRequest> updateBusinessProfileValidator,
            IValidator<UpdateBusinessDocumentsRequest> updateDocumentsValidator,
            IValidator<UpdateBusinessServiceAreasRequest> updateServiceAreasValidator)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _preferenceRepository = preferenceRepository;
            _businessProductTypeRepository = businessProductTypeRepository;
            _businessServiceAreaRepository = businessServiceAreaRepository;
            _bankAccountRepository = bankAccountRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _profileValidator = profileValidator;
            _surveyValidator = surveyValidator;
            _updateUsernameValidator = updateUsernameValidator;
            _updatePhoneValidator = updatePhoneValidator;
            _updateAvatarValidator = updateAvatarValidator;
            _updateBankValidator = updateBankValidator;
            _updateBusinessProfileValidator = updateBusinessProfileValidator;
            _updateDocumentsValidator = updateDocumentsValidator;
            _updateServiceAreasValidator = updateServiceAreasValidator;
        }

        public async Task<Result<string>> SubmitBusinessProfileAsync(
            Guid userId,
            SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken = default)
        {

            var validationResult = await _profileValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<string>.Fail(ValidationErrors.InvalidRequest(errorMessage));
            }

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
            var validationResult = await _surveyValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Fail(ValidationErrors.InvalidRequest(errorMessage));
            }

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

        public async Task<Result<BusinessProfileDetailDto>> GetBusinessProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result<BusinessProfileDetailDto>.Fail(new Error("User.NotFound", "Không tìm thấy người dùng."));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result<BusinessProfileDetailDto>.Fail(new Error("BusinessProfile.NotFound", "Chưa có hồ sơ doanh nghiệp."));

            var bankAccount = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);
            var documents = await _businessDocumentRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var serviceAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

            var detail = new BusinessProfileDetailDto
            {
                BusinessProfileId = profile.BusinessProfileId,
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,

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
                ReputationScore = profile.ReputationScore,

                BankAccount = bankAccount != null ? new BankAccountDto
                {
                    BankCode = bankAccount.BankCode ?? string.Empty,
                    BankName = bankAccount.BankName ?? string.Empty,
                    AccountNumber = bankAccount.AccountNumber ?? string.Empty,
                    AccountName = bankAccount.AccountName ?? string.Empty,
                    VerifyStatus = bankAccount.VerifyStatus ?? 0
                } : null,

                Documents = documents.Select(d => new BusinessDocumentResponseDto
                {
                    BusinessDocumentId = d.BusinessDocumentId,
                    DocumentType = d.DocumentType,
                    DocumentUrl = d.DocumentUrl ?? string.Empty
                }).ToList(),

                ServiceAreas = serviceAreas.Select(sa => new BusinessServiceAreaResponseDto
                {
                    City = sa.City ?? string.Empty,
                    District = sa.District ?? string.Empty,
                    Ward = sa.Ward ?? string.Empty
                }).ToList()
            };

            return Result<BusinessProfileDetailDto>.Success(detail);
        }

        public async Task<Result> UpdateUsernameAsync(Guid userId, UpdateUsernameRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateUsernameValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var cleanUsername = request.Username.Trim();

            var isTaken = await _userRepository.ExistsByUsernameAsync(cleanUsername, userId, cancellationToken);
            if (isTaken)
                return Result.Fail(new Error("User.UsernameExists", "Tên đăng nhập này đã được sử dụng."));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result.Fail(new Error("User.NotFound", "Không tìm thấy thông tin người dùng."));

            user.Username = cleanUsername;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }


        public async Task<Result> UpdatePhoneNumberAsync(Guid userId, UpdatePhoneNumberRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updatePhoneValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result.Fail(new Error("User.NotFound", "Không tìm thấy thông tin người dùng."));

            user.PhoneNumber = request.PhoneNumber.Trim();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }


        public async Task<Result> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateAvatarValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result.Fail(new Error("User.NotFound", "Không tìm thấy thông tin người dùng."));

            user.AvatarUrl = request.AvatarUrl.Trim();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateBankAccountAsync(Guid userId, UpdateBankAccountRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateBankValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

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
                var newBank = new bank_account
                {
                    UserBankId = Guid.NewGuid(),
                    UserId = userId,
                    BankCode = request.BankCode.Trim(),
                    BankName = request.BankName.Trim(),
                    AccountNumber = request.AccountNumber.Trim(),
                    AccountName = request.AccountName.Trim().ToUpper(),
                    VerifyStatus = (int)BankVerifyStatus.Unverified,
                    CreatedAt = DateTime.UtcNow
                };
                await _bankAccountRepository.AddAsync(newBank, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UpdateApprovedBusinessProfileAsync(Guid userId, UpdateApprovedBusinessProfileRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateBusinessProfileValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            profile.BusinessName = request.BusinessName.Trim();
            profile.FullName = request.FullName?.Trim();
            profile.BusinessDescription = request.BusinessDescription?.Trim();
            profile.TaxCode = request.TaxCode.Trim();
            profile.BusinessAddress = request.BusinessAddress.Trim();
            profile.Ward = request.Ward.Trim();
            profile.City = request.City.Trim();
            profile.IdentityNumber = request.IdentityNumber.Trim();
            profile.OperatingScope = request.OperatingScope?.Trim();
            profile.UpdatedAt = DateTime.UtcNow;

            _businessProfileRepository.Update(profile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateBusinessDocumentsAsync(Guid userId, UpdateBusinessDocumentsRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateDocumentsValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _businessDocumentRepository.DeleteAllByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

                var now = DateTime.UtcNow;
                var newDocs = request.Documents.Select(d => new business_document
                {
                    BusinessDocumentId = Guid.NewGuid(),
                    BusinessProfileId = profile.BusinessProfileId,
                    DocumentType = d.DocumentType,
                    DocumentUrl = d.DocumentUrl.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList();

                await _businessDocumentRepository.AddRangeAsync(newDocs, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi xảy ra khi cập nhật tài liệu cho BusinessProfileId: {ProfileId}", profile.BusinessProfileId);
                throw;
            }
        }

        public async Task<Result> UpdateBusinessServiceAreasAsync(
            Guid userId,
            UpdateBusinessServiceAreasRequest request,
            CancellationToken cancellationToken = default)
        {
            // 1. Validate Input Payload
            var valResult = await _updateServiceAreasValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            // 2. Check Profile Existence
            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            // 3. Rào chắn Nghiệp vụ: Chỉ Enterprise mới được đăng ký Service Areas
            if (profile.BusinessModel != (int)BusinessModel.Enterprise)
            {
                return Result.Fail(new Error("BusinessProfile.InvalidModel", "Mô hình kinh doanh của bạn không hỗ trợ cấu hình khu vực thu gom mở rộng."));
            }

            // 4. Transaction Wipe & Bulk Re-insert
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Xoá danh sách địa bàn thu gom cũ
                await _businessServiceAreaRepository.DeleteAllByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

                // Chèn danh sách địa bàn thu gom mới
                if (request.ServiceAreas != null && request.ServiceAreas.Any())
                {
                    var now = DateTime.UtcNow;
                    var serviceAreas = request.ServiceAreas.Select(sa => new business_service_area
                    {
                        BusinessServiceAreaId = Guid.NewGuid(),
                        BusinessProfileId = profile.BusinessProfileId,
                        City = sa.City.Trim(),
                        District = sa.District.Trim(),
                        Ward = sa.Ward.Trim(),
                        Priority = 0,
                        CreatedAt = now
                    }).ToList();

                    await _businessServiceAreaRepository.AddRangeAsync(serviceAreas, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi xảy ra khi cập nhật khu vực hoạt động cho BusinessProfileId: {ProfileId}", profile.BusinessProfileId);
                throw;
            }
        }

    }
}

