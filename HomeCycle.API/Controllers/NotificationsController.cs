using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                    throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

                return userId;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMine(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
        {
            var result = await _notificationService.GetMineAsync(
                CurrentUserId,
                request,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(
            CancellationToken cancellationToken)
        {
            var result =
                await _notificationService.GetUnreadCountAsync(
                    CurrentUserId,
                    cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid notificationId,
            CancellationToken cancellationToken)
        {
            var result = await _notificationService.MarkAsReadAsync(
                CurrentUserId,
                notificationId,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            CancellationToken cancellationToken)
        {
            var result =
                await _notificationService.MarkAllAsReadAsync(
                    CurrentUserId,
                    cancellationToken);

            return HandleResult(result);
        }

        #region PRIVATE HELPERS

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        #endregion
    }
}
