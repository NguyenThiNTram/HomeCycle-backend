using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HomeCycle.API.Middlewares
{
    public class ActiveUserRequirement : IAuthorizationRequirement { }

    public class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly IUserRepository _userRepository;

        public ActiveUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, ActiveUserRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Fail();
                return;
            }

            // Query DB mỗi request — chi phí chấp nhận được vì đây chỉ 1 lần lookup theo PK (rất nhanh),
            // và là cách DUY NHẤT để "thu hồi" quyền truy cập của JWT đã phát hành trước đó.
            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null || user.Status != UserStatus.Active)
            {
                context.Fail(new AuthorizationFailureReason(this, "Tài khoản đã bị khóa hoặc không tồn tại."));
                return;
            }

            context.Succeed(requirement);
        }


    }
}
