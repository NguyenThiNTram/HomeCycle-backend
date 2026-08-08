using HomeCycle.Application.DTOs.Responses.Shippings;
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
        private readonly IGhnService ghnAddressService;

        public GHNController(IGhnService ghnAddressService)
        {
            this.ghnAddressService = ghnAddressService;
        }

        [HttpGet("provinces")]
        [SwaggerOperation(Summary = "Lấy danh sách tỉnh/thành theo GHN")]
        [ProducesResponseType(
        typeof(IReadOnlyList<GhnProvinceResponse>),
        StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<GhnProvinceResponse>>>
        GetProvinces(CancellationToken cancellationToken)
        {
            var result = await ghnAddressService.GetProvincesAsync(
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("provinces/{provinceId:int:min(1)}/districts")]
        [SwaggerOperation(
            Summary = "Lấy danh sách quận/huyện theo ProvinceID của GHN")]
        [ProducesResponseType(
            typeof(IReadOnlyList<GhnDistrictResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<GhnDistrictResponse>>>
            GetDistricts(
                int provinceId,
                CancellationToken cancellationToken)
        {
            var result = await ghnAddressService.GetDistrictsAsync(
                provinceId,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("districts/{districtId:int:min(1)}/wards")]
        [SwaggerOperation(
            Summary = "Lấy danh sách phường/xã theo DistrictID của GHN")]
        [ProducesResponseType(
            typeof(IReadOnlyList<GhnWardResponse>),
            StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<GhnWardResponse>>>
            GetWards(
                int districtId,
                CancellationToken cancellationToken)
        {
            var result = await ghnAddressService.GetWardsAsync(
                districtId,
                cancellationToken);

            return Ok(result);
        }
    }
}
