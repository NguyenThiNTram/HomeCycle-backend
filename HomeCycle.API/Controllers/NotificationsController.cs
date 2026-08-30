using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
        [SwaggerOperation(
            Summary = "Lấy danh sách thông báo của người dùng",
            Description =
                "Trả về danh sách thông báo thuộc người dùng đang đăng nhập, " +
                "có hỗ trợ phân trang và sắp xếp thông báo mới nhất trước. " +
                "API được dùng để hiển thị trang hoặc hộp danh sách thông báo."
        )]
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
        [SwaggerOperation(
            Summary = "Lấy số lượng thông báo chưa đọc",
            Description =
                "Trả về tổng số thông báo chưa đọc của người dùng đang đăng nhập. " +
                "FE sử dụng kết quả để hiển thị badge trên biểu tượng thông báo."
        )]
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
        [SwaggerOperation(
            Summary = "Đánh dấu một thông báo đã đọc",
            Description =
                "Đánh dấu thông báo được chỉ định là đã đọc. " +
                "Người dùng chỉ được cập nhật thông báo thuộc tài khoản của mình. " +
                "FE nên gọi API khi người dùng mở hoặc chọn thông báo."
        )]
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
        [SwaggerOperation(
            Summary = "Đánh dấu tất cả thông báo đã đọc",
            Description =
                "Đánh dấu toàn bộ thông báo chưa đọc của người dùng hiện tại " +
                "thành đã đọc. API được sử dụng cho chức năng " +
                "\"Đánh dấu tất cả đã đọc\" trên giao diện."
        )]
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
