using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
using HomeCycle.Application.Interfaces.Generics;
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

        public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IJwtService jwtService, IMapper mapper, IConfiguration configuration, IValidator<RegisterPersonalRequest> validator, IValidator<LoginPersonalRequest> loginValidator, IPersonalProfileRepository personalProfileRepository)
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

        public async Task<Result<AuthResponse>> RegisterPersonalAsync(RegisterPersonalRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Result<AuthResponse>.Fail(ValidationErrors.InvalidRequest(string.Join(", ", errors)));
            }

            var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (emailExists)
                return Result<AuthResponse>.Fail(AuthErrors.EmailExists);

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
                    Email = request.Email.Trim().ToLower(),
                    IsEmailVerified = false,
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                var accessToken = _jwtService.GenerateAccessToken(newUser);
                var refreshToken = _jwtService.GenerateRefreshToken();

                return Result<AuthResponse>.Success(
                new AuthResponse
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
                }
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        //public async Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        //{
        //    if (string.IsNullOrWhiteSpace(refreshToken))
        //        return Result<LoginResponseDto>.Fail(AuthErrors.InvalidRefreshToken);

        //    var token = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);

        //    if (token == null || token.ExpiredAt <= DateTime.UtcNow || token.RevokedAt != null)
        //        return Result<LoginResponseDto>.Fail(AuthErrors.ExpiredRefreshToken);

        //    var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);

        //    if (token.RevokedAt != null)
        //        return Result<LoginResponseDto>.Fail(AuthErrors.RevokedRefreshToken);

        //    if (user == null) 
        //        return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

        //    //var newAccessToken = _jwtService.GenerateAccessToken(user);
        //    var newRefreshToken = _jwtService.GenerateRefreshToken();
        //    var now = DateTime.UtcNow;

        //    token.RevokedAt = now;
        //    token.ReplacedByToken = newRefreshToken;

        //    await _userRepository.AddRefreshTokenAsync(new refresh_token
        //    {
        //        RefreshTokenId = Guid.NewGuid(),
        //        UserId = user.UserId,
        //        Token = newRefreshToken,
        //        ExpiredAt = now.AddDays(7),
        //        CreatedAt = now
        //    }, cancellationToken);

        //    await _unitOfWork.SaveChangesAsync(cancellationToken);

        //    return Result<LoginResponseDto>.Success(new LoginResponseDto
        //    {
        //        Message = "Refresh token successful.",
        //        AccessToken = _jwtService.GenerateAccessToken(user),
        //        RefreshToken = newRefreshToken
        //    });
        //}

        public async Task<Result<LoginResponseDto>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidRefreshToken);

            // 1. Lấy token cũ lên (EF Core bắt đầu theo dõi thực thể này)
            var token = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);

            // 2. Gom tất cả điều kiện kiểm tra Token hợp lệ vào 1 chỗ cho sạch code
            if (token == null || token.ExpiredAt <= DateTime.UtcNow || token.RevokedAt != null)
                return Result<LoginResponseDto>.Fail(AuthErrors.ExpiredRefreshToken);

            // 3. Lấy thông tin User sở hữu token
            var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
            if (user == null)
                return Result<LoginResponseDto>.Fail(AuthErrors.InvalidCredential);

            // 4. Chuẩn bị dữ liệu cho token mới
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            // 5. Cập nhật trạng thái Token cũ trực tiếp (Thay đổi trạng thái trên RAM)
            token.RevokedAt = now;
            token.ReplacedByToken = newRefreshToken;

            // LƯU Ý: Tuyệt đối KHÔNG gọi _userRepository.UpdateRefreshToken(token) ở đây nữa!

            // 6. Thêm bản ghi Token mới vào cơ sở dữ liệu
            await _userRepository.AddRefreshTokenAsync(new refresh_token
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = user.UserId,
                Token = newRefreshToken,
                ExpiredAt = now.AddDays(7),
                CreatedAt = now
            }, cancellationToken);

            // 7. Thực thi lưu xuống Database (EF Core tự gom cụm Update token cũ và Insert token mới vào 1 Transaction)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                Message = "Refresh token successful.",
                AccessToken = _jwtService.GenerateAccessToken(user),
                RefreshToken = newRefreshToken
            });
        }

    }
}
