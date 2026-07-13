using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Users;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Users;
using HomeCycle.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Personals
{
    public class PersonalProfileService : IPersonalProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PersonalProfileService> _logger;

        private readonly IValidator<UpdatePersonalProfileRequest> _updateProfileValidator;
        private readonly IValidator<UpdateAvatarRequest> _updateAvatarValidator;
        private readonly IValidator<UpdateIdCardRequest> _updateIdCardValidator;
        private readonly IValidator<UpdateBankAccountRequest> _updateBankAccountValidator;

        public PersonalProfileService(
            IUserRepository userRepository,
            IPersonalProfileRepository personalProfileRepository,
            IBankAccountRepository bankAccountRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PersonalProfileService> logger,
            IValidator<UpdatePersonalProfileRequest> updateProfileValidator,
            IValidator<UpdateAvatarRequest> updateAvatarValidator,
            IValidator<UpdateIdCardRequest> updateIdCardValidator,
            IValidator<UpdateBankAccountRequest> updateBankAccountValidator)
        {
            _userRepository = userRepository;
            _personalProfileRepository = personalProfileRepository;
            _bankAccountRepository = bankAccountRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _updateProfileValidator = updateProfileValidator;
            _updateAvatarValidator = updateAvatarValidator;
            _updateIdCardValidator = updateIdCardValidator;
            _updateBankAccountValidator = updateBankAccountValidator;
        }

        public async Task<Result<PersonalProfileResponse>> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.UserNotFound);

            var profile = await _personalProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.ProfileNotFound);

            var bankAccount = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);

            var response = _mapper.Map<PersonalProfileResponse>(user); // Base info
            _mapper.Map(profile, response);

            if (bankAccount != null)
                response.BankAccount = _mapper.Map<BankAccountDto>(bankAccount);

            return Result<PersonalProfileResponse>.Success(response);
        }

        public async Task<Result<string>> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest file, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateAvatarValidator.ValidateAsync(file, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<string>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<string>.Fail(ProfileErrors.UserNotFound);

            user.AvatarUrl = file.AvatarUrl.Trim();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(user.AvatarUrl);
        }

        public async Task<Result> UpdateBankAsync(Guid userId, UpdateBankAccountRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateBankAccountValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) return Result.Fail(AuthErrors.InvalidCredential);

            var bank = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);

            if (bank is null)
            {
                bank = new bank_account
                {
                    UserBankId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    VerifyStatus = 0
                };

                _mapper.Map(request, bank);
                await _bankAccountRepository.AddAsync(bank, cancellationToken);
            }
            else
            {
                // Đã có
                _mapper.Map(request, bank);
                //await _bankAccountRepository.UpdateAsync(bank, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateIdentityAsync(Guid userId, UpdateIdCardRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateIdCardValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<PersonalProfileResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.UserNotFound);

            var profile = await _personalProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile is null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.ProfileNotFound);

            _mapper.Map(request, profile);

            // Đổi CCCD => cần kiểm duyệt lại, reset trạng thái xác minh
            profile.VerificationStatus = 0; // 0 = chờ duyệt
            profile.VerifiedBy = null;
            profile.VerifiedAt = null;

            //await _personalProfileRepository.UpdateAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var bankAccount = await _bankAccountRepository.GetByUserIdAsync(userId, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UpdateProfileAsync(Guid userId, UpdatePersonalProfileRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<PersonalProfileResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.UserNotFound);

            var profile = await _personalProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile is null)
                return Result<PersonalProfileResponse>.Fail(ProfileErrors.ProfileNotFound);

            // Cập nhật user
            _mapper.Map(request, user);
            _mapper.Map(request, profile);

            //await _userRepository.UpdateAsync(user, cancellationToken);
            //await _personalProfileRepository.UpdateAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
