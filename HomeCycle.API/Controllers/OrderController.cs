using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("buyer")]
        public async Task<IActionResult> GetMyOrdersAsBuyer(
            [FromQuery] OrderSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.GetMyOrdersAsync(currentUserId, isSeller: false, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("seller")]
        public async Task<IActionResult> GetMyOrdersAsSeller(
            [FromQuery] OrderSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.GetMyOrdersAsync(currentUserId, isSeller: true, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetDetail(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.GetDetailAsync(orderId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("agreement/{agreementId:guid}")]
        public async Task<IActionResult> GetByAgreement(Guid agreementId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.GetByAgreementAsync(agreementId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
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
