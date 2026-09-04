using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/conversations")]
    [ApiController]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationController(
            IConversationService conversationService)
        {
            _conversationService = conversationService;
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
            Summary = "Lấy danh sách hội thoại của tôi",
            Description =
                "Dùng cho màn hình Inbox hoặc danh sách chat chính. " +
                "Mỗi Conversation đại diện cho cuộc trò chuyện giữa hai người dùng " +
                "và có thể chứa nhiều phiên Negotiation. " +
                "Danh sách được sắp xếp theo LastActivityAt giảm dần nên page 1 " +
                "chứa các hội thoại hoạt động gần nhất. " +
                "Mỗi phần tử bao gồm người đối thoại, tin nhắn mới nhất, " +
                "Negotiation chứa tin nhắn đó và tổng số tin chưa đọc của người gọi API."
        )]
        [ProducesResponseType(typeof(PagedResult<ConversationListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMine([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidTokenResponse();

            var result = await _conversationService.GetMineAsync(userId, request, cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("{conversationId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết một hội thoại",
            Description =
                "Dùng khi FE mở trực tiếp hoặc tải lại màn hình Conversation. " +
                "API trả về người đối thoại, tin nhắn mới nhất, Negotiation chứa tin nhắn mới nhất, " +
                "tổng unread và thời gian hoạt động gần nhất. " +
                "Chỉ một trong hai người thuộc Conversation mới được truy cập. " +
                "API không trả toàn bộ tin nhắn; FE sử dụng endpoint /messages để lấy timeline."
        )]
        [ProducesResponseType(typeof(ConversationListItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid conversationId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidTokenResponse();

            var result = await _conversationService.GetByIdAsync(userId, conversationId, cancellationToken);

            return HandleResult(result);
        }


        /// <summary>
        /// Lấy timeline tổng hợp của một Conversation.
        /// </summary>
        [HttpGet("{conversationId:guid}/messages")]
        [SwaggerOperation(
            Summary = "Lấy timeline tin nhắn của hội thoại",
            Description =
                "Dùng cho màn hình chat tổng hợp giữa hai người dùng. " +
                "Timeline chứa tin nhắn của tất cả Negotiation thuộc Conversation, " +
                "không chỉ một phiên thương lượng riêng lẻ. " +
                "Page 1 là nhóm tin nhắn mới nhất; các tin trong mỗi page được trả từ cũ đến mới " +
                "để FE có thể hiển thị trực tiếp trong khung chat. " +
                "Mỗi MessageResponse vẫn chứa NegotiationId để FE xác định tin nhắn thuộc phiên nào. " +
                "Endpoint này chỉ đọc lịch sử, không dùng để gửi tin."
        )]
        [ProducesResponseType(typeof(PagedResult<MessageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTimeline([FromRoute] Guid conversationId, [FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidTokenResponse();

            var result = await _conversationService.GetTimelineAsync(userId, conversationId, request, cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("{conversationId:guid}/negotiations")]
        [SwaggerOperation(
            Summary = "Lấy danh sách phiên thương lượng trong hội thoại",
            Description =
                "Dùng để hiển thị các phiên thương lượng đã phát sinh giữa hai người dùng. " +
                "Một Conversation có thể chứa nhiều Negotiation vì hai người có thể thương lượng " +
                "nhiều bài đăng hoặc giao dịch khác nhau. " +
                "Mỗi phần tử chứa bài đăng, Offer hiện tại, trạng thái Negotiation, " +
                "thời gian tin nhắn gần nhất và số tin chưa đọc của riêng phiên đó. " +
                "FE sử dụng NegotiationId để mở đúng màn hình thương lượng và thực hiện " +
                "Text, Counter, Accept, Reject hoặc Cancel."
        )]
        [ProducesResponseType(typeof(PagedResult<NegotiationListItemResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNegotiations([FromRoute] Guid conversationId, [FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidTokenResponse();

            var result = await _conversationService.GetNegotiationsAsync(userId, conversationId, request, cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("{conversationId:guid}/read")]
        [SwaggerOperation(
            Summary = "Đánh dấu hội thoại đã đọc",
            Description =
                "Dùng khi người dùng mở hoặc đọc màn hình Conversation. " +
                "API đánh dấu đã đọc cho tất cả tin nhắn chưa đọc do người còn lại gửi " +
                "trong toàn bộ Conversation, bao gồm nhiều Negotiation khác nhau. " +
                "Tin nhắn do chính người gọi gửi không bị thay đổi. " +
                "Thao tác có tính idempotent: gọi lại khi không còn tin chưa đọc vẫn thành công. " +
                "Sau khi cập nhật, backend phát SignalR ConversationMessagesRead và " +
                "ConversationUpdated để FE đồng bộ badge unread trên màn hình Inbox."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid conversationId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
                return InvalidTokenResponse();

            var result = await _conversationService.MarkAsReadAsync(userId, conversationId, cancellationToken);

            return HandleResult(result);
        }

        #region helpers

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            if (result.Data is null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Invalid service response",
                        Detail = "Service trả về trạng thái thành công nhưng không có dữ liệu."
                    });
            }

            // Chỉ trả Data, đúng với schema đã khai báo trong Swagger.
            return Ok(result.Data);
        }

        private IActionResult HandleResult(Result result)
        {
            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok();
        }

        private IActionResult MapErrorToResponse(Error error)
        {
            // ConversationService hiện đang tạm sử dụng
            // NegotiationErrors cho NotFound và Forbidden
            if (Equals(error, NegotiationErrors.NotFound))
                return NotFound(error);

            if (Equals(error, NegotiationErrors.Forbidden))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    error);
            }

            return BadRequest(error);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var claim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return Guid.TryParse(claim, out userId);
        }

        private IActionResult InvalidTokenResponse()
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Token không hợp lệ hoặc thiếu thông tin định danh người dùng."
                });
        }

        #endregion
    }
}
