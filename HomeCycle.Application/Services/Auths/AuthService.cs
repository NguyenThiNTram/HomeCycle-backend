using AutoMapper;
using FluentValidation;
using Google.Apis.Auth;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Application.Interfaces.Services.Auths;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using MathNet.Numerics.Statistics.Mcmc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IValidator<LoginPersonalRequest> _loginPersonalValidator;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly ILogger<AuthService> _logger;
        private readonly IFileStorageService _fileStorageService;

        public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtService jwtService, IMapper mapper, IConfiguration configuration, IValidator<RegisterPersonalRequest> validator, IValidator<LoginPersonalRequest> _loginPersonalValidator, IValidator<LoginRequest> loginValidator, IPersonalProfileRepository personalProfileRepository, IOtpRepository otpRepository, IEmailService emailService, IBankAccountRepository bankAccountRepository, ILogger<AuthService> logger, IFileStorageService fileStorageService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
            _validator = validator;
            _personalProfileRepository = personalProfileRepository;
            _loginPersonalValidator = _loginPersonalValidator;
            _loginValidator = loginValidator;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _bankAccountRepository = bankAccountRepository;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }

        public async Task<Result<LoginResponseDto>> LoginPersonalAsync(LoginPersonalRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _loginPersonalValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<LoginResponseDto>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null || user.Role != UserRole.Personal)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Password);

            if (!isPasswordValid)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            await _userRepository.AddRefreshTokenAsync(
                new refresh_token
                {
                    RefreshTokenId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Token = refreshToken,
                    ExpiredAt = now.AddDays(7),
                    CreatedAt = now
                });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new LoginResponseDto
            {
                Message = "Login personal successful.",
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return Result<LoginResponseDto>.Success(response);

        }

        //login chung cho tất cả các loại user (Personal, Business, Moderator, Admin)
        public async Task<Result<LoginResponseDto>> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<LoginResponseDto>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Deleted)
            {
                return Result<LoginResponseDto>.Fail(AuthErrors.AccountSuspended);
            }

            var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Password);
            if (!isPasswordValid)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            await _userRepository.AddRefreshTokenAsync(
                new refresh_token
                {
                    RefreshTokenId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Token = refreshToken,
                    ExpiredAt = now.AddDays(7),
                    CreatedAt = now
                }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role.ToString(), // Tự động lấy Role của User (Personal/Business/Moderator/Admin)
            };

            return Result<LoginResponseDto>.Success(response);
        }

        public async Task<Result<AuthResponse>> RegisterPersonalAsync(string registrationToken, RegisterPersonalRequest request, CancellationToken cancellationToken = default)
        {
            // Validate Token & lấy Data
            var email = _jwtService.ValidateRegistrationTokenAndGetEmail(registrationToken);
            if (string.IsNullOrEmpty(email))
                return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest(
                    "The registration session is invalid or has expired. Please re-verify your email"));

            var normalizedEmail = email.Trim().ToLower();

            var googleAvatar = _jwtService.GetAvatarFromRegistrationToken(registrationToken);
            string? finalAvatarUrl = googleAvatar;

            // xử lý avatar (Lưu vào thư mục "avatars")
            if (request.AvatarUrl != null)
            {
                using var stream = request.AvatarUrl.OpenReadStream();
                var storedFileName = await _fileStorageService.UploadFileAsync(stream, request.AvatarUrl.FileName, "avatars");
                finalAvatarUrl = _fileStorageService.GetFileUrl(storedFileName, "avatars");
            }

            // xử lý CCCD/CMND (Lưu vào thư mục bảo mật "legal-documents")
            //var frontIdCardUrl = await UploadFileHelperAsync(request.FrontIDCardImage, "legal-documents", cancellationToken);
            //var backIdCardUrl = await UploadFileHelperAsync(request.BackIDCardImage, "legal-documents", cancellationToken);

            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
            if (emailExists)
                return Result<AuthResponse>.Fail(AuthErrors.EmailExists);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var normalizedUsername = request.Username.Trim();

            var usernameExists = await _userRepository.ExistsByUsernameAsync(request.Username,cancellationToken);
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
                    AvatarUrl = finalAvatarUrl?.Trim(),
                    Role = UserRole.Personal,
                    Status = UserStatus.Active,
                    CreatedAt = now
                };

                await _userRepository.AddAsync(newUser, cancellationToken);

                string? frontIdCardUrl = null;
                string? backIdCardUrl = null;

                if (request.FrontIDCardImage != null)
                {
                    using var stream = request.FrontIDCardImage.OpenReadStream();

                    frontIdCardUrl = await _fileStorageService.UploadFileAsync(
                        stream,
                        request.FrontIDCardImage.FileName,
                        $"identities/{newUser.UserId}/front_id_card");
                }

                if (request.BackIDCardImage != null)
                {
                    using var stream = request.BackIDCardImage.OpenReadStream();

                    backIdCardUrl = await _fileStorageService.UploadFileAsync(
                        stream,
                        request.BackIDCardImage.FileName,
                        $"identities/{newUser.UserId}/back_id_card");
                }

                var personalProfile = new personal_profile
                {
                    PersonalProfileId = Guid.NewGuid(),
                    UserId = newUser.UserId,
                    FullName = request.FullName?.Trim(),
                    RepresentativeCode = request.RepresentativeCode?.Trim(),
                    RepresentativeName = request.RepresentativeName?.Trim(),
                    RepresentativeDob = request.RepresentativeDob,
                    RepresentativeAddress = request.RepresentativeAddress?.Trim(),
                    FrontIDCardImage = frontIdCardUrl,
                    BackIDCardImage = backIdCardUrl,
                    VerificationStatus = 0,
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

                await _otpRepository.UpdateUserIdAsync(normalizedEmail, newUser.UserId, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<AuthResponse>.Success(new AuthResponse
                {
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
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
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

        public async Task<Result<GoogleAuthResponseDto>> ExecuteGoogleLoginAsync(string idToken, CancellationToken cancellationToken = default)
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
                var normalizedEmail = payload.Email.Trim().ToLower();

                var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

                if (user != null)
                {
                    // user đã tồn tại
                    if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Deleted)
                        return Result<GoogleAuthResponseDto>.Fail(AuthErrors.AccountSuspended);

                    var accessToken = _jwtService.GenerateAccessToken(user);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var now = DateTime.UtcNow;

                    await _userRepository.AddRefreshTokenAsync(new refresh_token
                    {
                        RefreshTokenId = Guid.NewGuid(),
                        UserId = user.UserId,
                        Token = refreshToken,
                        ExpiredAt = now.AddDays(7),
                        CreatedAt = now
                    }, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<GoogleAuthResponseDto>.Success(new GoogleAuthResponseDto
                    {
                        IsNewUser = false,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    });
                }
                else
                {
                    // user mới (đăng kí)
                    // Tạo một token đăng ký đặc biệt, đóng gói Email và Avatar từ Google vào Claims
                    // Bạn có thể tái sử dụng cơ chế của GenerateRegistrationToken hoặc tạo riêng
                    string externalRegisterToken = _jwtService.GenerateRegistrationToken(email: normalizedEmail,
                        avatarUrl: payload.Picture,
                        provider: "Google");

                    return Result<GoogleAuthResponseDto>.Success(new GoogleAuthResponseDto
                    {
                        IsNewUser = true,
                        ExternalRegisterToken = externalRegisterToken
                    });
                }
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Token từ Google không hợp lệ hoặc đã hết hạn.");
                return Result<GoogleAuthResponseDto>.Fail(ValidationErrors.InvalidRequest("Token from Google is invalid or expired!"));
            }
        }

        public async Task<Result<string>> SendOtpAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail);

            if (emailExists)
                return Result<string>.Fail(AuthErrors.EmailExists);

            var otpCode =Random.Shared.Next(100000, 999999).ToString();

            var otp = new otp
            {
                OtpId = Guid.NewGuid(),
                Email = normalizedEmail,
                Code = otpCode,
                Purpose = "Register",
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt =DateTime.UtcNow.AddMinutes(5)
            };

            await _otpRepository.AddAsync(otp);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otpCode);

            return Result<string>.Success("OTP has been sent successfully.");
        }

        public async Task<Result<string>> VerifyOtpAsync(string email, string code)
        {
            var otp =await _otpRepository.GetValidOtpAsync(email,code);

            if (otp == null)
                return Result<string>.Fail(AuthErrors.InvalidOtp);

            otp.IsUsed = true;
            otp.UsedAt =DateTime.UtcNow;

            await _otpRepository.UpdateAsync(otp);

            // Registration Token sau khi OTP hợp lệ
            string registrationToken = _jwtService.GenerateRegistrationToken(email);

            await _unitOfWork.SaveChangesAsync();

            // Trả token này về cho Frontend
            return Result<string>.Success(registrationToken);
            //return true;
        }

        //-----------------------------------------------------------------------------------------------------
        // HELPER METHODS
        //-----------------------------------------------------------------------------------------------------
        private async Task<string?> UploadFileHelperAsync(IFormFile? file, string folderName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0) return null;

            // Kích hoạt luồng đọc stream an toàn
            using var stream = file.OpenReadStream();

            // Gọi Storage Service với folder tương ứng theo nghiệp vụ SRS
            var storedFileName = await _fileStorageService.UploadFileAsync(stream, file.FileName, folderName);

            // Trả về Full URL hiển thị trực tiếp
            return _fileStorageService.GetFileUrl(storedFileName, folderName);
        }
    }
}
