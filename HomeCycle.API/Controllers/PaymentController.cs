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
        public PaymentController(
            IPaymentService paymentService)
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

            return Ok(result.Data);
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
        [Authorize]
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

        //[HttpPost("order-settlements/{paymentId:guid}/payos/checkout")]
        //[Authorize]
        //public async Task<IActionResult> CreateOrderSettlementPayOsCheckout(
        //    Guid paymentId,
        //    [FromBody] PayOSCheckoutRequest request,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await _orderSettlementService.GeneratePayOsCheckoutUrlAsync(
        //        paymentId,
        //        GetUserIdFromToken(),
        //        request.ReturnUrl,
        //        request.CancelUrl,
        //        cancellationToken);

        //    return result.IsSuccess
        //        ? Ok(new { checkoutUrl = result.Data })
        //        : BadRequest(result.Error);
        //}

        //[HttpPost("order-settlements/{paymentId:guid}/wallet/checkout")]
        //[Authorize]
        //public async Task<IActionResult> ExecuteOrderSettlementWalletCheckout(
        //    Guid paymentId,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await _orderSettlementService.ExecuteWalletAsync(
        //        paymentId,
        //        GetUserIdFromToken(),
        //        cancellationToken);

        //    return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        //}

        //[HttpGet("order-settlements/{paymentId:guid}/status")]
        //[Authorize]
        //public async Task<IActionResult> SyncOrderSettlementStatus(
        //    Guid paymentId,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await _orderSettlementService.SyncPayOsStatusAsync(
        //        paymentId,
        //        GetUserIdFromToken(),
        //        cancellationToken);

        //    return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        //}


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
