using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Infrastructure.Externals.GHN;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GHNController : ControllerBase
    {
        private readonly IGhnService _ghnService;
        private readonly IGhnWebhookService _webhookService;
        private readonly ILogger<GHNController> _logger;

        public GHNController(IGhnService ghnService, IGhnWebhookService webhookService, ILogger<GHNController> logger)
        {
            _ghnService = ghnService;
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpGet("provinces")]
        [SwaggerOperation(Summary = "Lấy danh sách tỉnh/thành theo GHN")]
        [ProducesResponseType(typeof(IReadOnlyList<GhnProvinceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<GhnProvinceResponse>>> GetProvinces(
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ghnService.GetProvincesAsync(cancellationToken);
                return Ok(result);
            }
            catch (GhnApiException ex)
            {
                return HandleGhnException(ex);
            }
        }

        [HttpGet("provinces/{provinceId:int:min(1)}/districts")]
        [SwaggerOperation(Summary = "Lấy danh sách quận/huyện theo ProvinceID của GHN")]
        [ProducesResponseType(typeof(IReadOnlyList<GhnDistrictResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<GhnDistrictResponse>>> GetDistricts(
            int provinceId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ghnService.GetDistrictsAsync(provinceId, cancellationToken);
                return Ok(result);
            }
            catch (GhnApiException ex)
            {
                return HandleGhnException(ex);
            }
        }

        [HttpGet("districts/{districtId:int:min(1)}/wards")]
        [SwaggerOperation(Summary = "Lấy danh sách phường/xã theo DistrictID của GHN")]
        [ProducesResponseType(typeof(IReadOnlyList<GhnWardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<GhnWardResponse>>> GetWards(
            int districtId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ghnService.GetWardsAsync(districtId, cancellationToken);
                return Ok(result);
            }
            catch (GhnApiException ex)
            {
                return HandleGhnException(ex);
            }
        }

        [HttpPost("calculate-fee")]
        [SwaggerOperation(
        Summary = "Tính phí vận chuyển dựa trên địa chỉ và thông số gói hàng")]
            [ProducesResponseType(
        typeof(GhnFeeQuoteResponse),
        StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<GhnFeeQuoteResponse>> CalculateFee([FromBody] CalculateGhnFeeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ghnService.GetShippingFeeAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (GhnApiException ex)
            {
                return HandleGhnException(ex);
            }
        }

        [HttpPost("create-order")]
        [SwaggerOperation(
        Summary = "Tạo đơn hàng GHN")]
        [ProducesResponseType(typeof(GhnCreateOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<GhnCreateOrderResponse>> CreateOrder([FromBody] GhnCreateOrderRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _ghnService.CreateOrderAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (GhnApiException ex)
            {
                return HandleGhnException(ex);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Route("webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> HandleAsync([FromBody] GhnWebhookRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _webhookService.ProcessAsync(
                    request,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    // GHN yêu cầu HTTP 200 sau khi xử lý thành công.
                    return Ok(new
                    {
                        success = true
                    });
                }

                var error = result.Error ??
                    new Error( "GhnWebhook.UnknownError", "Không thể xử lý webhook GHN.");

                return error.Code switch
                {
                    "GhnWebhook.InvalidPayload" => BadRequest(error),
                    "GhnWebhook.InvalidShop" => Unauthorized(error),
                    "GhnWebhook.ShipmentNotFound" => NotFound(error),
                    "GhnWebhook.OrderCodeConflict" => Conflict(error),
                    "GhnWebhook.GhnUnavailable" or "GhnWebhook.EmptyStatus" =>
                        StatusCode(
                            StatusCodes.Status503ServiceUnavailable,
                            error),

                    _ => BadRequest(error)
                };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError( exception, "Lỗi không mong muốn khi xử lý webhook GHN.");

                // Non-200 để GHN thực hiện retry.
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new Error( "GhnWebhook.ProcessingFailed", "Hệ thống tạm thời chưa thể xử lý webhook GHN."));
            }
        }

        private ActionResult HandleGhnException(GhnApiException ex)
        {
            // Kiểm tra nếu nội dung thông báo từ GHN báo lỗi không tồn tại hoặc tham số sai, ép về HTTP 400
            bool isClientError = ex.Message.Contains("khong ton tai", System.StringComparison.OrdinalIgnoreCase) ||
                                 ex.CodeMessage == "INVALID_PARAMETER";

            int finalStatusCode = isClientError ? StatusCodes.Status400BadRequest : (int)ex.StatusCode;

            return StatusCode(finalStatusCode, new
            {
                Title = "Lỗi dịch vụ giao hàng (GHN)",
                Status = finalStatusCode,
                Detail = ex.Message,
                GhnCode = ex.CodeMessage
            });
        }
    }
}
