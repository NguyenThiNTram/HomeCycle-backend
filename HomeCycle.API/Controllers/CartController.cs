using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Carts;
using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.Interfaces.Services.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
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
            Summary = "Lấy danh sách sản phẩm trong giỏ hàng",
            Description = "Trả về toàn bộ bài đăng đang có trong giỏ hàng của người dùng hiện tại cùng tổng số lượng và tổng giá trị."
        )]
        [ProducesResponseType(typeof(Result<CartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
        {
            var result = await _cartService.GetAsync(CurrentUserId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{postId:guid}")]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Thêm bài đăng vào giỏ hàng",
            Description = "Thêm một bài đăng bán đang hoạt động vào giỏ hàng của người dùng hiện tại."
        )]
        [ProducesResponseType(typeof(Result<CartItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CartItemResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddToCart(
            [FromRoute] Guid postId,
            [FromBody] AddToCartRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await _cartService.AddAsync(CurrentUserId, postId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("{cartItemId:guid}")]
        [SwaggerOperation(
            Summary = "Xóa 1 sản phẩm khỏi giỏ hàng",
            Description = "Xóa một bài đăng cụ thể khỏi giỏ hàng của người dùng hiện tại."
        )]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveFromCart(
            [FromRoute] Guid cartItemId,
            CancellationToken cancellationToken)
        {
            var result = await _cartService.RemoveAsync(CurrentUserId, cartItemId, cancellationToken);
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
