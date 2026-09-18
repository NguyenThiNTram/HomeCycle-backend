using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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

        [HttpGet("{agreementId:guid}/quote")]
        [Authorize]
        public async Task<IActionResult> GetPaymentQuote([FromRoute] Guid agreementId, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();
            var result = await _paymentService.GetPaymentQuoteAsync(agreementId, userId, ct);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == "Auth.Forbidden")
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        result.Error);
                if (result.Error?.Code == "Agreement.NotFound") return NotFound(result.Error.Message);

                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost("payos/checkout/{agreementId}")]
        [Authorize]
        public async Task<IActionResult> CreatePayOSCheckout([FromRoute] Guid agreementId, [FromBody] PayOSCheckoutRequest request, CancellationToken ct)
        {
            var userId = GetUserIdFromToken();

            var result = await _paymentService.GeneratePayOSCheckoutUrlAsync(agreementId, userId, request.ReturnUrl, request.CancelUrl, ct);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == "Auth.Forbidden")
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        result.Error);
                if (result.Error?.Code == "Agreement.NotFound") return NotFound(result.Error.Message);
                if (result.Error?.Code == "Payment.ActiveCheckoutExists")
                    return Conflict(result.Error);

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
                if (result.Error?.Code == "Auth.Forbidden")
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        result.Error);
                if (result.Error?.Code == "Agreement.NotFound") return NotFound(result.Error.Message);
                if (result.Error?.Code == "Payment.ActiveCheckoutExists")
                    return Conflict(result.Error);

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

        [HttpPost("subscriptions/{packageId:guid}/payos/checkout")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Thanh toán gói đăng ký bằng PayOS",
            Description = "Tạo Pending subscription, Payment và phiên checkout PayOS. Mỗi user chỉ được có tối đa một Pending hoặc Active subscription.")]
        [ProducesResponseType(typeof(SubscriptionPayOSCheckoutResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubscriptionPayOSCheckout(
            [FromRoute] Guid packageId,
            [FromBody] PayOSCheckoutRequest request,
            CancellationToken ct)
        {
            var result = await _paymentService.CreateSubscriptionPayOSCheckoutAsync(
                packageId,
                GetUserIdFromToken(),
                request.ReturnUrl,
                request.CancelUrl,
                ct);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapSubscriptionPaymentError(result.Error);
        }

        [HttpPost("subscriptions/{packageId:guid}/wallet/checkout")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Thanh toán gói đăng ký bằng ví nội bộ",
            Description = "Trừ Available Balance của user, cộng Platform_Revenue và kích hoạt subscription trong cùng database transaction.")]
        [ProducesResponseType(typeof(SubscriptionPaymentStatusResponseDto), StatusCodes.Status200OK)]

        public async Task<IActionResult> SubscriptionWalletCheckout(
            [FromRoute] Guid packageId,
            CancellationToken ct)
        {
            var result = await _paymentService.ExecuteSubscriptionWalletPaymentAsync(
                packageId,
                GetUserIdFromToken(),
                ct);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapSubscriptionPaymentError(result.Error);
        }


        [HttpGet("subscriptions/{subscriptionId:guid}/status")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Đồng bộ trạng thái thanh toán subscription",
            Description = "Trả trạng thái Payment và Subscription. Nếu Payment PayOS còn Pending, backend đồng bộ trạng thái mới nhất từ PayOS trước khi trả kết quả.")]
        [ProducesResponseType(typeof(SubscriptionPaymentStatusResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SyncSubscriptionPaymentStatus(
            [FromRoute] Guid subscriptionId,
            CancellationToken ct)
        {
            var result = await _paymentService.SyncSubscriptionPaymentStatusAsync(
                subscriptionId,
                GetUserIdFromToken(),
                ct);

            return result.IsSuccess
                ? Ok(result.Data)
                : MapSubscriptionPaymentError(result.Error);
        }


        private IActionResult MapSubscriptionPaymentError(Error? error)
        {
            return error?.Code switch
            {
                "SubscriptionPackage.NotFound" or
                "UserSubscription.NotFound" or
                "Payment.NotFound" or
                "AUTH_USER_NOT_FOUND"
                    => NotFound(error),

                "Auth.Forbidden" or
                "UserSubscription.RoleNotEligible" or
                "UserSubscription.UserInactive"
                    => StatusCode(
                        StatusCodes.Status403Forbidden,
                        error),

                "UserSubscription.OpenExists" or
                "UserSubscription.InvalidStatus" or
                "SubscriptionPackage.Inactive" or
                "UserSubscription.InsufficientBalance"
                    => Conflict(error),

                "Payment.InvalidAmount" or
                "Payment.InvalidRedirectUrl"
                    => BadRequest(error),

                "Payment.InvalidGatewayResponse"
                    => StatusCode(
                        StatusCodes.Status502BadGateway,
                        error),

                "Payment.OrderCodeConflict"
                    => StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        error),

                "UserSubscription.WalletNotFound" or
                "UserSubscription.PlatformRevenueWalletNotFound" or
                "Payment.CreateFailed" or
                "WalletPayment.TransactionFailed"
                    => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        error),

                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    error)
            };
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
