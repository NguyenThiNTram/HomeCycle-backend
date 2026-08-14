using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.Interfaces.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("payos/checkout/{agreementId}")]
        [Authorize]
        public async Task<IActionResult> CreatePayOSCheckout([FromRoute] Guid agreementId, [FromBody] PayOSCheckoutRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();

            var result = await _paymentService.GeneratePayOSCheckoutUrlAsync(agreementId, userId, request.ReturnUrl, request.CancelUrl, ct);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == "Auth.Forbidden") return Forbid(result.Error.Message);
                if (result.Error?.Code == "Agreement.NotFound") return NotFound(result.Error.Message);

                return BadRequest(result.Error);
            }

            return Ok(new { checkoutUrl = result.Data });
        }


        [HttpPost("wallet/checkout/{agreementId}")]
        [Authorize]
        public async Task<IActionResult> WalletCheckout([FromRoute] Guid agreementId, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();
            var result = await _paymentService.ExecuteWalletPaymentAsync(agreementId, userId, ct);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == "Auth.Forbidden") return Forbid(result.Error.Message);
                if (result.Error?.Code == "Agreement.NotFound") return NotFound(result.Error.Message);

                return BadRequest(result.Error);
            }

            return Ok(new { success = true, message = "Thanh toán bằng ví nội bộ thành công." });
        }

        [HttpPost("payos/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook(CancellationToken ct)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var webhookBody = await reader.ReadToEndAsync(ct);

                var result = await _paymentService.HandlePaymentWebhookAsync(webhookBody, ct);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Error);
                }

                return Ok(new { success = true });
            }
            catch (Exception)
            {
                // Thực tế nên inject ILogger vào Controller để log Exception này
                return StatusCode(500, "Internal server error processing webhook.");
            }
        }

        [HttpGet("{agreementId:guid}/status")]
        public async Task<IActionResult> SyncPaymentStatus(Guid agreementId, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();
            var result = await _paymentService.SyncPaymentStatusAsync(agreementId, userId, ct);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetMyPaymentHistory([FromQuery] PaymentHistorySearchRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();
            var result = await _paymentService.GetMyPaymentHistoryAsync(userId, request, ct);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }


        private Guid GetUserIdFromToken()
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("id")
                              ?? User.FindFirst("sub");

            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                throw new UnauthorizedAccessException("Token không chứa thông tin định danh người dùng.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Định dạng UserId trong Token không hợp lệ (Không phải Guid).");
            }

            return userId;
        }
    }
}
