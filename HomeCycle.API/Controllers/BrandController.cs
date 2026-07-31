using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Application.Interfaces.Services.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers
{
    [Route("api/brands")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách thương hiệu",
            Description = "Trả về danh sách tất cả thương hiệu có hỗ trợ tìm kiếm và phân trang."
        )]
        public async Task<IActionResult> GetBrands([FromQuery] BrandSearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _brandService.SearchAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("active")]
        [SwaggerOperation(
            Summary = "Lấy danh sách thương hiệu đang hoạt động",
            Description = "Trả về danh sách thương hiệu đang hoạt động (IsActive = true)."
        )]
        public async Task<IActionResult> GetActiveBrands([FromQuery] BrandSearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _brandService.GetActiveAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin thương hiệu theo ID",
            Description = "Trả về chi tiết thông tin của một thương hiệu theo ID."
        )]
        public async Task<IActionResult> GetBrand(Guid id, CancellationToken cancellationToken)
        {
            var result = await _brandService.GetByIdAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo thương hiệu mới",
            Description = "Tạo mới một thương hiệu sản phẩm trong hệ thống."
        )]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
        {
            var result = await _brandService.CreateBrandAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật thương hiệu",
            Description = "Cập nhật thông tin tên, mô tả hoặc trạng thái của thương hiệu theo ID."
        )]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
        {
            var result = await _brandService.UpdateBrandAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [SwaggerOperation(
            Summary = "Xóa thương hiệu",
            Description = "Xóa (hoặc vô hiệu hóa) một thương hiệu khỏi hệ thống theo ID."
        )]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken cancellationToken)
        {
            var result = await _brandService.DeleteBrandAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
