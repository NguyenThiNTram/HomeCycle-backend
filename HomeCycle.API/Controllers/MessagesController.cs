using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
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

        [HttpPost]
        [SwaggerOperation(
            Summary = "Gửi tin nhắn",
            Description =
                "Gửi tin nhắn văn bản trong phiên thương lượng. " +
                "Chỉ buyer hoặc seller của phiên thương lượng được phép gửi."
        )]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Send(
            Guid negotiationId,
            [FromBody] SendMessageRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _messageService.SendAsync(
                CurrentUserId,
                negotiationId,
                request,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy lịch sử tin nhắn",
            Description =
                "Lấy danh sách tin nhắn có phân trang. " +
                "Chỉ thành viên của phiên thương lượng được truy cập."
        )]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistory(
            Guid negotiationId,
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _messageService.GetHistoryAsync(
                CurrentUserId,
                negotiationId,
                request,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("read")]
        [SwaggerOperation(
            Summary = "Đánh dấu tin nhắn đã đọc",
            Description =
                "Đánh dấu toàn bộ tin nhắn chưa đọc do đối phương gửi " +
                "trong phiên thương lượng."
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _messageService.MarkAsReadAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            return NoContent();
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
