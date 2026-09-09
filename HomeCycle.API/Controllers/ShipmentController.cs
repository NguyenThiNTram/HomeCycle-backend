using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.Interfaces.Services.Shipments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/shipments")]
    [Authorize]
    public class ShipmentController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        [HttpPost("{shipmentId:guid}/seller-ready")]
        [SwaggerOperation(
            Summary = "Seller xác nhận đã chuẩn bị xong hàng",
            Description = "Áp dụng cho GHN, Seller tự giao và Buyer tự đến lấy hàng."
        )]
        public async Task<IActionResult> ConfirmSellerReady(
            Guid shipmentId,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _shipmentService.ConfirmSellerReadyAsync(
                shipmentId,
                currentUserId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapSellerReadyError(result.Error!);

            return Ok(result.Data);
        }

        private IActionResult MapSellerReadyError(Error error)
        {
            if (error == ShipmentErrors.NotFound ||
                error == OrderErrors.NotFound ||
                error == AgreementErrors.NotFound)
            {
                return NotFound(error);
            }

            if (error == ShipmentErrors.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, error);

            if (error == ShipmentErrors.SellerReadyNotAllowed ||
                error == ShipmentErrors.OrderMismatch)
            {
                return Conflict(error);
            }

            return BadRequest(error);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) ||
                !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException(
                    "Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");
            }

            return userId;
        }
    }
}
