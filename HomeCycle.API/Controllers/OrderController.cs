using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Application.Interfaces.Services.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IGhnTrackingSyncService _ghnTrackingSyncService;

        public OrderController(IOrderService orderService, IGhnTrackingSyncService ghnTrackingSyncService)
        {
            _orderService = orderService;
            _ghnTrackingSyncService = ghnTrackingSyncService;
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

        [HttpGet("{orderId:guid}/shipment-tracking")]
        [SwaggerOperation(
            Summary = "Lấy trạng thái vận chuyển GHN của đơn hàng",
            Description =
                "Chỉ Buyer hoặc Seller thuộc đơn hàng được phép xem. " +
                "Backend tự lấy GHNOrderCode và đồng bộ với GHN."
        )]
        public async Task<IActionResult> GetShipmentTracking(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _ghnTrackingSyncService.SyncByOrderIdAsync(
                orderId,
                currentUserId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapTrackingError(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("{orderId:guid}/confirm-handover")]
        public async Task<IActionResult> ConfirmHandover(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.ConfirmHandoverAsync(orderId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("{orderId:guid}/confirm-received")]
        public async Task<IActionResult> ConfirmReceived(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.ConfirmReceivedAsync(orderId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        private IActionResult MapTrackingError(Error? error)
        {
            if (error is null)
                return BadRequest();

            return error.Code switch
            {
                "Order.NotFound"
                    or "Agreement.NotFound"
                    or "Shipment.NotFound"
                    => NotFound(error),

                "Auth.Forbidden"
                    => StatusCode(
                        StatusCodes.Status403Forbidden,
                        error),

                "Shipment.GhnRecordNotFound"
                    or "Shipment.GhnOrderCodeMissing"
                    => Conflict(error),

                "Shipment.NotGhnDelivery"
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
