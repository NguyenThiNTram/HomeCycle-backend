using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Infrastructure.Externals.GHN;
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
        private readonly ILogger<GHNController> _logger;

        public GHNController(IGhnService ghnService, ILogger<GHNController> logger)
        {
            _ghnService = ghnService;
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
