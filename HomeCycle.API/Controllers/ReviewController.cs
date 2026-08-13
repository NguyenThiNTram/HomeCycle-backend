using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Reviews;
using HomeCycle.Application.Interfaces.Services.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("orders/{orderId:guid}")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Tạo đánh giá cho đơn hàng đã hoàn thành",
            Description =
                "Chỉ Buyer hoặc Seller thuộc đơn hàng (trạng thái Completed) mới được đánh giá. " +
                "Mỗi người chỉ được đánh giá 1 lần cho 1 đơn hàng. Rating từ 1 đến 5 sao. " +
                "Có thể upload tối đa 3 ảnh đính kèm (không bắt buộc)."
        )]
        public async Task<IActionResult> CreateReview(
            Guid orderId, [FromForm] CreateReviewRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _reviewService.CreateReviewAsync(orderId, request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        [HttpPut("{reviewId:guid}")]
        [SwaggerOperation(
            Summary = "Chỉnh sửa đánh giá",
            Description =
                "Chỉ tác giả đánh giá được sửa, và chỉ trong vòng 3 ngày kể từ khi gửi đánh giá. " +
                "Sau 3 ngày không thể chỉnh sửa. Đánh giá không thể bị xoá."
        )]
        public async Task<IActionResult> UpdateReview(
            Guid reviewId, [FromBody] UpdateReviewRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _reviewService.UpdateReviewAsync(reviewId, request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("{reviewId:guid}")]
        public async Task<IActionResult> GetById(Guid reviewId, CancellationToken cancellationToken)
        {
            var result = await _reviewService.GetByIdAsync(reviewId, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("orders/{orderId:guid}/mine")]
        [SwaggerOperation(Summary = "Lấy đánh giá của chính mình cho một đơn hàng")]
        public async Task<IActionResult> GetMyReviewForOrder(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _reviewService.GetMyReviewForOrderAsync(orderId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("orders/{orderId:guid}")]
        [SwaggerOperation(Summary = "Lấy danh sách đánh giá của một đơn hàng")]
        public async Task<IActionResult> GetReviewsByOrder(
            Guid orderId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _reviewService.GetReviewsByOrderAsync(
                orderId, currentUserId, pageNumber, pageSize, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("users/{userId:guid}")]
        [SwaggerOperation(Summary = "Lấy danh sách đánh giá người dùng nhận được")]
        public async Task<IActionResult> GetReviewsByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _reviewService.GetReviewsByUserAsync(userId, pageNumber, pageSize, cancellationToken);

            if (!result.IsSuccess)
                return MapError(result.Error);

            return Ok(result.Data);
        }

        private IActionResult MapError(Error? error)
        {
            if (error is null)
                return BadRequest();

            return error.Code switch
            {
                "Order.NotFound"
                    or "Agreement.NotFound"
                    or "Review.NotFound"
                    => NotFound(error),

                "Auth.Forbidden"
                    => StatusCode(StatusCodes.Status403Forbidden, error),

                "Order.NotCompleted"
                    or "Review.AlreadyExists"
                    or "Review.EditWindowExpired"
                    or "Validation.InvalidRequest"
                    => BadRequest(error),

                _ => BadRequest(error)
            };
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

            return userId;
        }
    }
}
