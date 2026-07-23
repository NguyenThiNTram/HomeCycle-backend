using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Users;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Users;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Auths
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonalProfileRepository _personalProfileRepository;
        private readonly IBusinessProfileRepository _businessProfileRepository;
        private readonly IPostRepository _postRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IPersonalProfileRepository personalProfileRepository,
            IBusinessProfileRepository businessProfileRepository,
            IPostRepository postRepository,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _personalProfileRepository = personalProfileRepository;
            _businessProfileRepository = businessProfileRepository;
            _postRepository = postRepository;
            _logger = logger;
        }

        public async Task<Result<PublicUserProfileResponse>> GetPublicProfileAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<PublicUserProfileResponse>.Fail(AuthErrors.UserNotFound);

            Console.WriteLine($"UserId: {userId}");

            if (user == null)
            {
                Console.WriteLine("USER NULL");
            }
            else
            {
                Console.WriteLine($"Role: {user.Role}");
            }

            if (user.Role == UserRole.Moderator || user.Role == UserRole.Admin)
            {
                return Result<PublicUserProfileResponse>.Fail(
                    new Error("Profile.NotPublic", "Tài khoản Quản trị / Kiểm duyệt không có hồ sơ giao dịch công khai."));
            }

            var activePostCount = await _postRepository.CountActiveByOwnerAsync(userId, cancellationToken);

            if (user.Role == UserRole.Business)
            {
                var business = await _businessProfileRepository.GetByUserIdAsync(userId, cancellationToken);
                if (business is null)
                    return Result<PublicUserProfileResponse>.Fail(AuthErrors.UserNotFound);

                return Result<PublicUserProfileResponse>.Success(new BusinessPublicProfileResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl,
                    ReputationScore = business.ReputationScore,
                    //IsVerified = business.VerificationStatus == (int)VerificationStatus.Verified,
                    JoinedAt = user.CreatedAt,
                    ActivePostCount = activePostCount,
                    BusinessName = business.BusinessName,
                    BusinessDescription = business.BusinessDescription,
                    BusinessAddress = business.BusinessAddress,
                    Ward = business.Ward,
                    City = business.City,
                    OperatingScope = business.OperatingScope,
                    BusinessModel = business.BusinessModel
                });
            }
            else
            {
                try
                {
                    var personal = await _personalProfileRepository.GetByUserIdAsync(
                        userId,
                        cancellationToken);

                    _logger.LogWarning("======> AFTER GET PERSONAL <======");

                    if (personal is null)
                    {
                        _logger.LogError("======> PERSONAL NULL <======");
                        return Result<PublicUserProfileResponse>.Fail(AuthErrors.UserNotFound);
                    }

                    _logger.LogWarning($"======> Personal Name: {personal.FullName} <======");
                    _logger.LogWarning($"======> Personal Score: {personal.ReputationScore} <======");

                    var response = new PersonalPublicProfileResponse
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl,
                        ReputationScore = personal.ReputationScore,
                        JoinedAt = user.CreatedAt,
                        ActivePostCount = activePostCount,
                        FullName = personal.FullName
                    };

                    _logger.LogWarning($"======> Personal Name: {personal.FullName} <======");

                    return Result<PublicUserProfileResponse>.Success(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "======> ERROR OCCURRED <======");
                    Console.WriteLine("ERROR:");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);

                    throw;
                }
            }
        }
    }
}
