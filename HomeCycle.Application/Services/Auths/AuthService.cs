using AutoMapper;
using FluentValidation;
using Google.Apis.Auth;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Auths
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IValidator<RegisterPersonalRequest> _validator;
        private readonly IValidator<LoginPersonalRequest> _loginValidator;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IBankAccountRepository _bankAccountRepository;

        public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtService jwtService, IMapper mapper, IConfiguration configuration, IValidator<RegisterPersonalRequest> validator, IValidator<LoginPersonalRequest> loginValidator, IPersonalProfileRepository personalProfileRepository, IOtpRepository otpRepository, IEmailService emailService, IBankAccountRepository bankAccountRepository)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
            _validator = validator;
            _personalProfileRepository = personalProfileRepository;
            _loginValidator = loginValidator;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _bankAccountRepository = bankAccountRepository;
        }

        public async Task<Result<LoginResponseDto>> LoginPersonalAsync(LoginPersonalRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Result<LoginResponseDto>.Fail(ValidationErrors.InvalidRequest(string.Join(", ", errors)));
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null || user.Role != UserRole.Personal)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Password);

            if (!isPasswordValid)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _userRepository.AddRefreshTokenAsync(
                new refresh_token
                {
                    RefreshTokenId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Token = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new LoginResponseDto
            {
                Message = "Login personal successful.",
                //User = _mapper.Map<AuthUserDto>(user),
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return Result<LoginResponseDto>.Success(response);

        }

        //public async Task<Result<AuthResponse>> RegisterPersonalAsync(RegisterPersonalRequest request, CancellationToken cancellationToken = default)
        //{
        //    var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        //    if (!validationResult.IsValid)
        //    {
        //        var errors = validationResult.Errors
        //            .Select(x => x.ErrorMessage)
        //            .ToList();

        //        return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest(string.Join(", ", errors)));
        //    }

        //    var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        //    if (emailExists)
        //        return Result<AuthResponse>.Fail(AuthErrors.EmailExists);

        //    var usernameExists = await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken);
        //    if (usernameExists)
        //        return Result<AuthResponse>.Fail(AuthErrors.UsernameExists);

        //    await _unitOfWork.BeginTransactionAsync();

        //    try
        //    {
        //        var now = DateTime.UtcNow;
        //        var newUser = new user
        //        {
        //            UserId = Guid.NewGuid(),
        //            Username = request.Username.Trim(),
        //            Email = request.Email.Trim().ToLower(),
        //            IsEmailVerified = false,
        //            Password = _passwordHasher.HashPassword(request.Password),
        //            PhoneNumber = request.PhoneNumber?.Trim(),
        //            AvatarUrl = request.AvatarUrl?.Trim(),
        //            Role = UserRole.Personal,
        //            Status = UserStatus.Active,
        //            CreatedAt = now
        //        };
        //        await _userRepository.AddAsync(newUser, cancellationToken);

        //        var personalProfile = new personal_profile
        //        {
        //            PersonalProfileId = Guid.NewGuid(),
        //            UserId = newUser.UserId,
        //            FullName = request.FullName?.Trim(),
        //            RepresentativeCode = request.RepresentativeCode?.Trim(),
        //            RepresentativeName = request.RepresentativeName?.Trim(),
        //            RepresentativeDob = request.RepresentativeDob,
        //            RepresentativeAddress = request.RepresentativeAddress?.Trim(),
        //            FrontIDCardImage = request.FrontIDCardImage,
        //            BackIDCardImage = request.BackIDCardImage,
        //            CreatedAt = now
        //        };
        //        await _personalProfileRepository.AddAsync(personalProfile, cancellationToken);

        //        await _unitOfWork.SaveChangesAsync(cancellationToken);
        //        await _unitOfWork.CommitTransactionAsync();

        //        var accessToken = _jwtService.GenerateAccessToken(newUser);
        //        var refreshToken = _jwtService.GenerateRefreshToken();

        //        return Result<AuthResponse>.Success(
        //        new AuthResponse
        //        {
        //            Message = "Register personal successful.",

        //            User = new AuthUserDto
        //            {
        //                UserId = newUser.UserId,
        //                Username = newUser.Username,
        //                Email = newUser.Email,
        //                PhoneNumber = newUser.PhoneNumber,
        //                AvatarUrl = newUser.AvatarUrl,
        //                Role = newUser.Role,
        //                Status = newUser.Status,
        //                IsEmailVerified = newUser.IsEmailVerified
        //            }
        //        }
        //        );
        //    }
        //    catch
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();
        //        throw;
        //    }
        //}

        public async Task<Result<AuthResponse>> RegisterPersonalAsync(string registrationToken, RegisterPersonalRequest request, CancellationToken cancellationToken = default)
        {
            //if (string.IsNullOrWhiteSpace(email))
            //    return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest("Email is required."));

            var email = _jwtService.ValidateRegistrationTokenAndGetEmail(registrationToken);
            if (string.IsNullOrEmpty(email))
                return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest("Phiên đăng ký không hợp lệ hoặc đã hết hạn. Vui lòng xác thực lại email."));

            var normalizedEmail = email.Trim().ToLower();

            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
            if (emailExists)
                return Result<AuthResponse>.Fail(AuthErrors.EmailExists);

            //var isEmailVerified = await _otpRepository.IsEmailVerifiedAsync(normalizedEmail, cancellationToken);
            //if (!isEmailVerified)
            //    return Result<AuthResponse>.Fail(AuthErrors.EmailNotVerified);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Result<AuthResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", errors)));
            }

            var usernameExists = await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken);
            if (usernameExists)
                return Result<AuthResponse>.Fail(AuthErrors.UsernameExists);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                var newUser = new user
                {
                    UserId = Guid.NewGuid(),
                    Username = request.Username.Trim(),
                    Email = normalizedEmail,
                    IsEmailVerified = true,
                    Password = _passwordHasher.HashPassword(request.Password),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    AvatarUrl = request.AvatarUrl?.Trim(),
                    Role = UserRole.Personal,
                    Status = UserStatus.Active,
                    CreatedAt = now
                };

                await _userRepository.AddAsync(newUser, cancellationToken);

                var personalProfile = new personal_profile
                {
                    PersonalProfileId = Guid.NewGuid(),
                    UserId = newUser.UserId,
                    FullName = request.FullName?.Trim(),
                    RepresentativeCode = request.RepresentativeCode?.Trim(),
                    RepresentativeName = request.RepresentativeName?.Trim(),
                    RepresentativeDob = request.RepresentativeDob,
                    RepresentativeAddress = request.RepresentativeAddress?.Trim(),
                    FrontIDCardImage = request.FrontIDCardImage,
                    BackIDCardImage = request.BackIDCardImage,
                    CreatedAt = now
                };

                await _personalProfileRepository.AddAsync(personalProfile, cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.AccountNumber))
                {
                    var bankAccount = new bank_account
                    {
                        UserBankId = Guid.NewGuid(),
                        UserId = newUser.UserId,
                        BankCode = request.BankCode?.Trim(),
                        BankName = request.BankName?.Trim(),
                        AccountNumber = request.AccountNumber.Trim(),
                        AccountName = request.AccountName?.Trim(),
                        VerifyStatus = 0,
                        CreatedAt = now
                    };

                    await _bankAccountRepository.AddAsync(bankAccount, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<AuthResponse>.Success(new AuthResponse
                {
                    Message = "Register personal successful.",
                    User = new AuthUserDto
                    {
                        UserId = newUser.UserId,
                        Username = newUser.Username,
                        Email = newUser.Email,
                        PhoneNumber = newUser.PhoneNumber,
                        AvatarUrl = newUser.AvatarUrl,
                        Role = newUser.Role,
                        Status = newUser.Status,
                        IsEmailVerified = newUser.IsEmailVerified
                    }
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidRefreshToken);

            var token = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);

            if (token == null || token.ExpiredAt <= DateTime.UtcNow || token.RevokedAt != null)
                return Result<LoginResponseDto>.Fail(AuthErrors.ExpiredRefreshToken);

            var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user == null)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            token.RevokedAt = now;
            token.ReplacedByToken = newRefreshToken;

            await _userRepository.AddRefreshTokenAsync(new refresh_token
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = user.UserId,
                Token = newRefreshToken,
                ExpiredAt = now.AddDays(7),
                CreatedAt = now
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                Message = "Refresh token successful.",
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = newRefreshToken
            });
        }

        public async Task<string> ExecuteGoogleLoginAsync(string idToken)
        {
            try
            {
                var clientId = _configuration["GoogleAuth:web:client_id"];
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { clientId! },

                    // Cho phép lệch tối đa 30 phút để tránh lỗi đồng hồ không đồng bộ
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(30)
                };

                // Xác thực token với Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return $"Login successful by email: {payload.Email}";
            }
            catch (InvalidJwtException)
            {
                throw new Exception("Token from Google is invalid or expired!");
            }
        }

        public async Task SendOtpAsync(string email)
        {

            var otpCode =Random.Shared.Next(100000, 999999).ToString();

            var otp = new otp
            {
                OtpId = Guid.NewGuid(),
                Email = email,
                Code = otpCode,
                Purpose = "Register",
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt =DateTime.UtcNow.AddMinutes(5)
            };

            await _otpRepository.AddAsync(otp);
            await _emailService.SendOtpEmailAsync(email, otpCode);
        }

        public async Task<Result<string>> VerifyOtpAsync(string email, string code)
        {
            var otp =await _otpRepository.GetValidOtpAsync(email,code);

            if (otp == null)
                return Result<string>.Fail(AuthErrors.InvalidOtp);

            otp.IsUsed = true;
            otp.UsedAt =DateTime.UtcNow;

            // Registration Token sau khi OTP hợp lệ
            string registrationToken = _jwtService.GenerateRegistrationToken(email);

            await _unitOfWork.SaveChangesAsync();

            // Trả token này về cho Frontend
            return Result<string>.Success(registrationToken);
            //return true;
        }
    }
}
