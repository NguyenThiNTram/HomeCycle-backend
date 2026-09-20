using HomeCycle.Application.Commons.Errors;
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
                "Backend trả trạng thái đang lưu trong HomeCycle; trạng thái được cập nhật qua webhook GHN."
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

        [HttpPost("{orderId:guid}/cancel")]
        [SwaggerOperation(
            Summary = "Hủy đơn hàng trước khi giao dịch bắt đầu",
            Description = "Buyer hoặc Seller có thể hủy Inspection Order trước khi có check-in, hoặc Collection Order trước khi Seller xác nhận đã chuẩn bị hàng. Sau mốc này đơn chỉ có thể xử lý qua Dispute. Phí GHN đã phát sinh không được hoàn."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _orderService.CancelOrderAsync(orderId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return MapOrderCancellationError(result.Error!);

            return Ok(result.Data);
        }


        [HttpPost("{orderId:guid}/confirm-return")]
        [SwaggerOperation(
            Summary = "Buyer xác nhận đã trả hàng",
            Description = "Ghi nhận Buyer đã trả hàng và bắt đầu thời hạn phản hồi của Seller."
        )]
        public async Task<IActionResult> ConfirmReturnByBuyer(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _orderService.ConfirmReturnByBuyerAsync(
                orderId,
                currentUserId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapOrderReturnError(result.Error!);

            return Ok(result.Data);
        }

        [HttpPost("{orderId:guid}/confirm-return-received")]
        [SwaggerOperation(
            Summary = "Seller xác nhận đã nhận lại hàng",
            Description = "Hoàn tất trả hàng và refund toàn bộ số tiền còn được nền tảng giữ cho Order."
        )]
        public async Task<IActionResult> ConfirmReturnReceivedBySeller(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _orderService.ConfirmReturnReceivedBySellerAsync(
                orderId,
                currentUserId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapOrderReturnError(result.Error!);

            return Ok(result.Data);
        }


        private IActionResult MapOrderCancellationError(Error error)
        {
            if (error == OrderErrors.NotFound ||
                error == AgreementErrors.NotFound ||
                error == AppointmentErrors.NotFound ||
                error == OrderErrors.ShipmentNotFound)
            {
                return NotFound(error);
            }

            if (error == OrderErrors.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, error);

            if (error == OrderErrors.InvalidStatus ||
                error == OrderErrors.ActiveDisputeBlocksCancellation ||
                error == OrderErrors.CancellationNotAllowed ||
                error.Code.StartsWith("Payment."))
            {
                return Conflict(error);
            }

            return BadRequest(error);
        }

        private IActionResult MapOrderReturnError(Error error)
        {
            if (error == OrderErrors.NotFound ||
                error == AgreementErrors.NotFound ||
                error == DisputeErrors.NotFound)
            {
                return NotFound(error);
            }

            if (error == OrderErrors.Forbidden ||
                error == DisputeErrors.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden, error);
            }

            if (error == OrderErrors.ReturnConfirmationNotAllowed ||
                error.Code == "Order.ReturnDeadlineExpired" ||
                error == OrderErrors.NotDisputing)
            {
                return Conflict(error);
            }

            if (error.Code is "Ghn.CancellationPending" or "Ghn.CancellationRefused") return Conflict(error);
            return BadRequest(error);
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

                "Ghn.TrackingChanged"
                    or "Shipment.GhnRecordNotFound"
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
