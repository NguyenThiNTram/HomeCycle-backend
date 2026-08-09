using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Banks;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
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
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BusinessProfileService> _logger;
        private readonly IValidator<SubmitBusinessProfileRequest> _profileValidator;
        private readonly IValidator<SubmitBusinessSurveyRequest> _surveyValidator;
        private readonly IValidator<UpdateUsernameRequest> _updateUsernameValidator;
        private readonly IValidator<UpdatePhoneNumberRequest> _updatePhoneValidator;
        private readonly IValidator<UpdateAvatarRequest> _updateAvatarValidator;
        private readonly IValidator<UpdateBankAccountRequest> _updateBankValidator;
        private readonly IValidator<UpdateBusinessDocumentsRequest> _updateDocumentsValidator;
        private readonly IValidator<UpdateBusinessServiceAreasRequest> _updateServiceAreasValidator;
        private readonly IValidator<UpdateIdentityRequest> _updateIdentityValidator;
        private readonly IValidator<UpdateBusinessRegistrationRequest> _updateBusinessRegistrationValidator;
        private readonly IValidator<BusinessServiceAreaRequestDto> _serviceAreaRequestDtoValidator;

        public BusinessProfileService(
            IBusinessProfileRepository businessProfileRepository,
            IBusinessDocumentRepository businessDocumentRepository,
            IBusinessProcurementPreferenceRepository preferenceRepository,
            IBusinessProductTypeRepository businessProductTypeRepository,
            IBusinessServiceAreaRepository businessServiceAreaRepository,
            IBankAccountRepository bankAccountRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BusinessProfileService> logger,
            IValidator<SubmitBusinessProfileRequest> profileValidator,
            IValidator<SubmitBusinessSurveyRequest> surveyValidator,
            IValidator<UpdateUsernameRequest> updateUsernameValidator,
            IValidator<UpdatePhoneNumberRequest> updatePhoneValidator,
            IValidator<UpdateAvatarRequest> updateAvatarValidator,
            IValidator<UpdateBankAccountRequest> updateBankValidator,
            IValidator<UpdateBusinessDocumentsRequest> updateDocumentsValidator,
            IValidator<UpdateBusinessServiceAreasRequest> updateServiceAreasValidator,
            IFileStorageService fileStorageService,
            IValidator<UpdateIdentityRequest> updateIdentityValidator,
            IValidator<UpdateBusinessRegistrationRequest> updateBusinessRegistrationValidator,
            IValidator<BusinessServiceAreaRequestDto> serviceAreaRequestDtoValidator)
        {
            _businessProfileRepository = businessProfileRepository;
            _businessDocumentRepository = businessDocumentRepository;
            _preferenceRepository = preferenceRepository;
            _businessProductTypeRepository = businessProductTypeRepository;
            _businessServiceAreaRepository = businessServiceAreaRepository;
            _bankAccountRepository = bankAccountRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _profileValidator = profileValidator;
            _surveyValidator = surveyValidator;
            _updateUsernameValidator = updateUsernameValidator;
            _updatePhoneValidator = updatePhoneValidator;
            _updateAvatarValidator = updateAvatarValidator;
            _updateBankValidator = updateBankValidator;
            _updateDocumentsValidator = updateDocumentsValidator;
            _updateServiceAreasValidator = updateServiceAreasValidator;
            _fileStorageService = fileStorageService;
            _updateIdentityValidator = updateIdentityValidator;
            _updateBusinessRegistrationValidator = updateBusinessRegistrationValidator;
            _serviceAreaRequestDtoValidator = serviceAreaRequestDtoValidator;
        }

        public async Task<Result<string>> SubmitBusinessProfileAsync(
            Guid userId,
            SubmitBusinessProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // 0. TRUY VẤN TRƯỚC KHI VALIDATE - vì validator cần biết IsResubmit + ExistingActiveDocTypes
            //    để quyết định document nào bắt buộc phải gửi lại (Hướng B).
            var existingProfile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            bool isResubmit = existingProfile != null;

            var existingActiveDocTypes = new List<int>();
            if (isResubmit)
            {
                var activeDocs = await _businessDocumentRepository.GetActiveByProfileIdAsync(
                    existingProfile!.BusinessProfileId, cancellationToken);
                existingActiveDocTypes = activeDocs.Select(d => d.DocumentType).ToList();
            }

            // 1. VALIDATE - nối dây RootContextData để validator (bạn đã viết đúng) đọc được ngữ cảnh
            var validationContext = new ValidationContext<SubmitBusinessProfileRequest>(request);
            validationContext.RootContextData["IsResubmit"] = isResubmit;
            validationContext.RootContextData["ExistingActiveDocTypes"] = existingActiveDocTypes;

            var validationResult = await _profileValidator.ValidateAsync(validationContext, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<string>.Fail(ValidationErrors.InvalidRequest(errorMessage));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Guid targetProfileId;

                if (existingProfile == null)
                {
                    // KỊCH BẢN A: NỘP HỒ SƠ LẦN ĐẦU
                    var newProfile = _mapper.Map<business_profile>(request);
                    targetProfileId = newProfile.BusinessProfileId = Guid.NewGuid();
                    newProfile.UserId = userId;
                    newProfile.Status = (int)BusinessProfileStatus.Pending;
                    newProfile.ReputationScore = 100;
                    newProfile.CreatedAt = now;
                    newProfile.UpdatedAt = now;

                    await _businessProfileRepository.AddAsync(newProfile, cancellationToken);

                    var bankAccount = _mapper.Map<bank_account>(request);
                    bankAccount.UserBankId = Guid.NewGuid();
                    bankAccount.UserId = userId;
                    bankAccount.VerifyStatus = VerifyStatus.Verified;
                    bankAccount.CreatedAt = now;

                    await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);

                    // Lần đầu: chưa có document nào -> insert mới bình thường, tất cả đều active (ReplacedAt = null)
                    foreach (var docDto in request.Documents)
                    {
                        var doc = _mapper.Map<business_document>(docDto);
                        doc.BusinessDocumentId = Guid.NewGuid();
                        doc.BusinessProfileId = targetProfileId;
                        doc.CreatedAt = now;
                        doc.ReplacedAt = null; // ĐANG ACTIVE - KHÔNG được gán = now

                        using (var stream = docDto.DocumentUrl.OpenReadStream())
                        {
                            doc.DocumentUrl = await _fileStorageService.UploadFileAsync(
                                stream, docDto.DocumentUrl.FileName, $"business-documents/{targetProfileId}");
                        }

                        await _businessDocumentRepository.AddAsync(doc, cancellationToken);
                    }

                    // Service areas (nếu Enterprise)
                    if (request.BusinessModel == (int)BusinessModel.Enterprise && request.ServiceArea != null)
                    {
                        var area = _mapper.Map<business_service_area>(request.ServiceArea);
                        area.BusinessServiceAreaId = Guid.NewGuid();
                        area.BusinessProfileId = targetProfileId;
                        area.Priority = 0;
                        area.CreatedAt = now;

                        await _businessServiceAreaRepository.AddAsync(area, cancellationToken);
                    }
                }
                else
                {
                    // KỊCH BẢN B: RESUBMIT SAU KHI BỊ REJECTED
                    if (existingProfile.Status != (int)BusinessProfileStatus.Rejected)
                    {
                        return Result<string>.Fail(ValidationErrors.InvalidRequest(
                            "Your application is either pending approval or has already been approved. It cannot be edited at this time."));
                    }

                    targetProfileId = existingProfile.BusinessProfileId;

                    _mapper.Map(request, existingProfile);
                    existingProfile.Status = (int)BusinessProfileStatus.Pending;
                    existingProfile.UpdatedAt = now;
                    _businessProfileRepository.Update(existingProfile);

                    if (request.BusinessModel == (int)BusinessModel.Enterprise && request.ServiceArea != null)
                    {
                        var existingAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(targetProfileId, cancellationToken);
                        var primaryArea = existingAreas.FirstOrDefault();

                        if (primaryArea != null)
                        {
                            _mapper.Map(request.ServiceArea, primaryArea);
                            _businessServiceAreaRepository.Update(primaryArea);
                        }
                        else
                        {
                            var area = _mapper.Map<business_service_area>(request.ServiceArea);
                            area.BusinessServiceAreaId = Guid.NewGuid();
                            area.BusinessProfileId = targetProfileId;
                            area.Priority = 0;
                            area.CreatedAt = now;
                            await _businessServiceAreaRepository.AddAsync(area, cancellationToken);
                        }
                    }

                    // Bank account: upsert đúng chuẩn (map đè hoặc add), KHÔNG xóa - giữ nguyên logic gốc, đúng rồi.
                    var existingBank = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);
                    if (existingBank != null)
                    {
                        _mapper.Map(request, existingBank);
                        existingBank.VerifyStatus = VerifyStatus.Verified;
                        _bankAccountRepository.UpdateAsync(existingBank);
                    }
                    else
                    {
                        var bankAccount = _mapper.Map<bank_account>(request);
                        bankAccount.UserBankId = Guid.NewGuid();
                        bankAccount.UserId = userId;
                        bankAccount.VerifyStatus = VerifyStatus.Verified;
                        bankAccount.CreatedAt = now;
                        await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);
                    }

                    // Document: HƯỚNG B - chỉ soft-replace type nào có file mới trong request.
                    // Type không có trong request.Documents -> GIỮ NGUYÊN bản active cũ, không đụng tới.
                    foreach (var docDto in request.Documents)
                    {
                        await HandleDocumentSoftReplaceAsync(targetProfileId, docDto.DocumentType, docDto.DocumentUrl, cancellationToken);
                    }
                }

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

            var documents = await _businessDocumentRepository.GetActiveByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var serviceAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);


            // Map base từ profile trước, sau đó map đè bankAccount lên cùng object (giống pattern PersonalProfileService)
            var registrationDetail = _mapper.Map<BusinessRegistrationDetailDto>(profile);
            if (bankAccount != null)
                _mapper.Map(bankAccount, registrationDetail);

            registrationDetail.Documents = _mapper.Map<List<BusinessRegistrationDocumentDto>>(documents);
            registrationDetail.ServiceAreas = _mapper.Map<BusinessRegistrationServiceAreaDto>(serviceAreas.FirstOrDefault());

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

                    var newPreference = _mapper.Map<business_procurement_preference>(request);
                    newPreference.PreferenceId = Guid.NewGuid();
                    newPreference.BusinessProfileId = businessProfileId;
                    newPreference.CreatedAt = DateTime.UtcNow;

                    await _preferenceRepository.AddAsync(newPreference, cancellationToken); 
                }
                else
                {

                    _mapper.Map(request, domainPreference);
                    domainPreference.UpdatedAt = DateTime.UtcNow;

                    _preferenceRepository.Update(domainPreference); 
                }

                // 2. Diff-upsert danh mục loại sản phẩm (thay cho Delete-all + Insert-all)
                var existingProductTypes = await _businessProductTypeRepository.GetByProfileIdAsync(businessProfileId, cancellationToken);
                var incomingTypeIds = (request.ProductTypeIds ?? new List<Guid>()).Distinct().ToList();

                var existingTypeIdSet = existingProductTypes.Select(pt => pt.ProductTypeId).ToHashSet();
                var incomingTypeIdSet = incomingTypeIds.ToHashSet();

                // Xóa những cái không còn trong danh sách mới
                var toRemove = existingProductTypes
                    .Where(pt => !incomingTypeIdSet.Contains(pt.ProductTypeId))
                    .Select(pt => pt.BusinessProductTypeId)
                    .ToList();
                await _businessProductTypeRepository.DeleteRangeAsync(toRemove, cancellationToken);

                // Thêm những cái chưa có trong danh sách cũ - giữ nguyên (không đổi Priority) cho cái đã tồn tại
                var toAddIds = incomingTypeIds.Where(id => !existingTypeIdSet.Contains(id)).ToList();
                if (toAddIds.Any())
                {
                    var newProductTypes = new List<business_product_type>();
                    // Priority tiếp nối từ số lượng hiện có, tránh trùng
                    int priority = existingProductTypes.Count + 1;
                    foreach (var typeId in toAddIds)
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

            var response = _mapper.Map<BusinessSurveyDetailResponse>(preference);
            response.ProductTypeIds = productTypeIds;

            return Result<BusinessSurveyDetailResponse>.Success(response);
        }

        public async Task<Result<BusinessOnboardingStatusDto>> GetOnboardingStatusAsync(Guid userId, CancellationToken cancellationToken)
        {
            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            if (profile == null)
            {
                return Result<BusinessOnboardingStatusDto>.Success(new BusinessOnboardingStatusDto
                {
                    Status = BusinessOnboardingStatus.MissingProfile,
                    IsActionRequired = false,
                    Message = "Bạn chưa đăng ký hồ sơ doanh nghiệp. Hoàn tất đăng ký để mở khoá các tính năng dành cho doanh nghiệp.",
                    ActionRoute = BusinessOnboardingActionRoute.OnboardingForm
                });
            }

            if (profile.Status == (int)BusinessProfileStatus.Pending)
            {
                return Result<BusinessOnboardingStatusDto>.Success(new BusinessOnboardingStatusDto
                {
                    Status = BusinessOnboardingStatus.PendingApproval,
                    IsActionRequired = false,
                    Message = "Hồ sơ doanh nghiệp của bạn đang được xét duyệt. Chúng tôi sẽ thông báo ngay khi có kết quả."
                });
            }

            if (profile.Status == (int)BusinessProfileStatus.Rejected)
            {
                return Result<BusinessOnboardingStatusDto>.Success(new BusinessOnboardingStatusDto
                {
                    Status = BusinessOnboardingStatus.Rejected,
                    IsActionRequired = false,
                    Message = "Hồ sơ doanh nghiệp của bạn đã bị từ chối. Vui lòng kiểm tra lý do và nộp lại hồ sơ.",
                    RejectReason = profile.RejectReason,
                    ActionRoute = BusinessOnboardingActionRoute.OnboardingForm
                });
            }

            if (profile.Status == (int)BusinessProfileStatus.Approved)
            {
                bool hasSurvey = await _preferenceRepository.ExistsByBusinessProfileIdAsync(profile.BusinessProfileId, cancellationToken);
                if (!hasSurvey)
                {
                    return Result<BusinessOnboardingStatusDto>.Success(new BusinessOnboardingStatusDto
                    {
                        Status = BusinessOnboardingStatus.SurveyPending,
                        IsActionRequired = true,
                        Message = "Vui lòng hoàn tất khảo sát nhu cầu thu mua để tiếp tục sử dụng nền tảng với vai trò doanh nghiệp.",
                        ActionRoute = BusinessOnboardingActionRoute.SurveyForm
                    });
                }
            }

            return Result<BusinessOnboardingStatusDto>.Success(new BusinessOnboardingStatusDto
            {
                Status = BusinessOnboardingStatus.Completed,
                IsActionRequired = false
            });
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
            var documents = await _businessDocumentRepository.GetActiveByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            var serviceAreas = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);

            var detail = _mapper.Map<BusinessProfileDetailDto>(user);
            _mapper.Map(profile, detail);

            if (bankAccount != null)
                detail.BankAccount = _mapper.Map<BankAccountDto>(bankAccount);

            detail.Documents = _mapper.Map<List<BusinessDocumentResponseDto>>(documents);
            detail.ServiceAreas = _mapper.Map<List<BusinessServiceAreaResponseDto>>(serviceAreas);

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
            await _userRepository.UpdateAsync(user, cancellationToken);
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
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }


        public async Task<Result> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateAvatarValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<string>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<string>.Fail(ProfileErrors.UserNotFound);

            // 2. Đọc file stream và upload lên Firebase
            string storedFileName;
            using (var stream = request.AvatarUrl.OpenReadStream())
            {
                storedFileName = await _fileStorageService.UploadFileAsync(
                    stream,
                    request.AvatarUrl.FileName,
                    "avatars");
            }

            user.AvatarUrl = storedFileName;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(user.AvatarUrl);
        }

        public async Task<Result> UpdateBankAccountAsync(Guid userId, UpdateBankAccountRequest request, CancellationToken cancellationToken = default)
        {
            var valResult = await _updateBankValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var existingBank = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);

            if (existingBank != null)
            {

                _mapper.Map(request, existingBank);
                existingBank.VerifyStatus = VerifyStatus.Verified;

                _bankAccountRepository.UpdateAsync(existingBank);
            }
            else
            {

                var newBank = _mapper.Map<bank_account>(request);
                newBank.UserBankId = Guid.NewGuid();
                newBank.UserId = userId;
                newBank.VerifyStatus = VerifyStatus.Verified;
                newBank.CreatedAt = DateTime.UtcNow;

                await _bankAccountRepository.AddAsync(newBank, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }


        public async Task<Result>   UpdateBusinessDocumentsAsync(Guid userId, UpdateBusinessDocumentsRequest request, CancellationToken cancellationToken = default)
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


                var newDocs = new List<business_document>();
                foreach (var d in request.Documents)
                {
                    var doc = _mapper.Map<business_document>(d);
                    doc.BusinessDocumentId = Guid.NewGuid();
                    doc.BusinessProfileId = profile.BusinessProfileId;
                    doc.CreatedAt = now;
                    doc.ReplacedAt = now;

                    using (var stream = d.DocumentUrl.OpenReadStream())
                    {
                        doc.DocumentUrl = await _fileStorageService.UploadFileAsync(
                            stream,
                            d.DocumentUrl.FileName,
                            $"business-documents/{profile.BusinessProfileId}");
                    }

                    newDocs.Add(doc);
                }

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
            var valResult = await _updateServiceAreasValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            if (profile.BusinessModel != (int)BusinessModel.Enterprise)
            {
                return Result.Fail(new Error("BusinessProfile.InvalidModel", "Mô hình kinh doanh của bạn không hỗ trợ cấu hình khu vực thu gom mở rộng."));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;

                // DIFF-BASED UPSERT: so khớp theo bộ khóa tự nhiên (City, District, Ward)
                // vì request không có Id để đối chiếu trực tiếp.
                var existing = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
                var incoming = request.ServiceAreas ?? new List<BusinessServiceAreaRequestDto>();

                string Key(string city, string district, string ward) =>
                    $"{city.Trim().ToUpperInvariant()}|{district.Trim().ToUpperInvariant()}|{ward.Trim().ToUpperInvariant()}";

                var existingByKey = existing.ToDictionary(e => Key(e.City, e.Street, e.Ward), e => e);
                var incomingKeys = incoming.Select(i => Key(i.City, i.Street, i.Ward)).ToHashSet();

                // Xóa những bản ghi cũ KHÔNG còn trong danh sách mới
                var toRemove = existing.Where(e => !incomingKeys.Contains(Key(e.City, e.Street, e.Ward)))
                                        .Select(e => e.BusinessServiceAreaId)
                                        .ToList();
                await _businessServiceAreaRepository.DeleteRangeAsync(toRemove, cancellationToken);

                // Thêm những bản ghi mới CHƯA có trong danh sách cũ - giữ nguyên bản ghi trùng khóa (không đụng tới)
                var toAdd = incoming
                    .Where(i => !existingByKey.ContainsKey(Key(i.City, i.Street, i.Ward)))
                    .Select(sa =>
                    {
                        var area = _mapper.Map<business_service_area>(sa);
                        area.BusinessServiceAreaId = Guid.NewGuid();
                        area.BusinessProfileId = profile.BusinessProfileId;
                        area.Priority = 0;
                        area.CreatedAt = now;
                        return area;
                    }).ToList();

                if (toAdd.Any())
                    await _businessServiceAreaRepository.AddRangeAsync(toAdd, cancellationToken);

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

        public async Task<Result> UpdateIdentityAsync(Guid userId, UpdateIdentityRequest request, CancellationToken cancellationToken = default)
        {

            var validationResult = await _updateIdentityValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Fail(ValidationErrors.InvalidRequest(errorMessage));
            }


            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
                if (profile == null)
                    return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

                string oldIdentityName = profile.IdentityName;

                _mapper.Map(request, profile);
                profile.UpdatedAt = DateTime.UtcNow;
                _businessProfileRepository.Update(profile);

                if (request.CccdFront != null)
                    await HandleDocumentSoftReplaceAsync(profile.BusinessProfileId, 0, request.CccdFront, cancellationToken);

                if (request.CccdBack != null)
                    await HandleDocumentSoftReplaceAsync(profile.BusinessProfileId, 1, request.CccdBack, cancellationToken);


                // BẮT BUỘC CÓ SAVECHANGES TRƯỚC KHI COMMIT
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi UpdateIdentity cho UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<Result> UpdateBusinessRegistrationAsync(Guid userId, UpdateBusinessRegistrationRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Validate Input Payload cho Business Registration
            var validationResult = await _updateBusinessRegistrationValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Fail(ValidationErrors.InvalidRequest(errorMessage));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
                if (profile == null)
                    return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp của user này."));

                _mapper.Map(request, profile);
                profile.UpdatedAt = DateTime.UtcNow;
                _businessProfileRepository.Update(profile);

                if (request.BusinessRegistrationCertificate != null)
                {
                    await HandleDocumentSoftReplaceAsync(profile.BusinessProfileId, 2, request.BusinessRegistrationCertificate, cancellationToken);
                }

                // BẮT BUỘC CÓ SAVECHANGES
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi UpdateBusinessRegistration cho UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<Result<Guid>> CreateBusinessServiceAreaAsync(
            Guid userId,
            BusinessServiceAreaRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var valResult = await _serviceAreaRequestDtoValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result<Guid>.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result<Guid>.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            if (profile.BusinessModel != (int)BusinessModel.Enterprise)
                return Result<Guid>.Fail(new Error("BusinessProfile.InvalidModel", "Mô hình kinh doanh của bạn không hỗ trợ cấu hình khu vực thu gom mở rộng."));

            // Chặn trùng khu vực (cùng City/Street/Ward) để không phá vỡ Unique Index ux_business_service_area ở DB.
            var existing = await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken);
            bool isDuplicate = existing.Any(e =>
                string.Equals(e.City?.Trim(), request.City.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Street?.Trim(), request.Street.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Ward?.Trim(), request.Ward.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
                return Result<Guid>.Fail(new Error("BusinessServiceArea.Duplicate", "Khu vực hoạt động này đã tồn tại."));

            var area = _mapper.Map<business_service_area>(request);
            area.BusinessServiceAreaId = Guid.NewGuid();
            area.BusinessProfileId = profile.BusinessProfileId;
            area.Priority = existing.Count;
            area.CreatedAt = DateTime.UtcNow;

            await _businessServiceAreaRepository.AddAsync(area, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(area.BusinessServiceAreaId);
        }

        public async Task<Result> UpdateBusinessServiceAreaAsync(
            Guid userId,
            Guid businessServiceAreaId,
            BusinessServiceAreaRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var valResult = await _serviceAreaRequestDtoValidator.ValidateAsync(request, cancellationToken);
            if (!valResult.IsValid)
                return Result.Fail(ValidationErrors.InvalidRequest(string.Join(" | ", valResult.Errors.Select(e => e.ErrorMessage))));

            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            var area = await _businessServiceAreaRepository.GetByIdAsync(businessServiceAreaId, cancellationToken);
            if (area == null)
                return Result.Fail(new Error("BusinessServiceArea.NotFound", "Không tìm thấy khu vực hoạt động."));

            // Chốt chặn phân quyền: khu vực này phải thuộc chính hồ sơ doanh nghiệp của user đang gọi API.
            if (area.BusinessProfileId != profile.BusinessProfileId)
                return Result.Fail(new Error("Auth.Forbidden", "Bạn không có quyền chỉnh sửa khu vực hoạt động này."));

            var duplicated = (await _businessServiceAreaRepository.GetByProfileIdAsync(profile.BusinessProfileId, cancellationToken))
                .Any(e => e.BusinessServiceAreaId != businessServiceAreaId &&
                          string.Equals(e.City?.Trim(), request.City.Trim(), StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(e.Street?.Trim(), request.Street.Trim(), StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(e.Ward?.Trim(), request.Ward.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicated)
                return Result.Fail(new Error("BusinessServiceArea.Duplicate", "Khu vực hoạt động này đã tồn tại."));

            _mapper.Map(request, area);
            _businessServiceAreaRepository.Update(area);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> DeleteBusinessServiceAreaAsync(
            Guid userId,
            Guid businessServiceAreaId,
            CancellationToken cancellationToken = default)
        {
            var profile = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Fail(new Error("BusinessProfile.NotFound", "Không tìm thấy hồ sơ doanh nghiệp."));

            var area = await _businessServiceAreaRepository.GetByIdAsync(businessServiceAreaId, cancellationToken);
            if (area == null)
                return Result.Fail(new Error("BusinessServiceArea.NotFound", "Không tìm thấy khu vực hoạt động."));

            if (area.BusinessProfileId != profile.BusinessProfileId)
                return Result.Fail(new Error("Auth.Forbidden", "Bạn không có quyền xóa khu vực hoạt động này."));

            await _businessServiceAreaRepository.DeleteAsync(businessServiceAreaId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        private async Task HandleDocumentSoftReplaceAsync(Guid businessProfileId, int documentType, IFormFile file, CancellationToken cancellationToken)
        {
            using var stream = file.OpenReadStream();
            string folderName = $"business-documents/{businessProfileId}";
            string fileUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, folderName);

            var activeDoc = await _businessDocumentRepository.GetActiveByProfileIdAndTypeAsync(businessProfileId, documentType, cancellationToken);
            DateTime now = DateTime.UtcNow;

            if (activeDoc != null)
            {
                activeDoc.ReplacedAt = now;
                // Bắt buộc gọi Update vì chúng ta dùng pattern mapper tách biệt Domain/Infra
                _businessDocumentRepository.Update(activeDoc);
            }

            var newDoc = new business_document
            {
                BusinessDocumentId = Guid.NewGuid(),
                BusinessProfileId = businessProfileId,
                DocumentType = documentType,
                DocumentUrl = fileUrl,
                CreatedAt = now,
                ReplacedAt = null
            };

            await _businessDocumentRepository.AddAsync(newDoc, cancellationToken);
        }

    }
}

