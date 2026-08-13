using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Categories;
using HomeCycle.Application.Interfaces.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("get-all")]
        [SwaggerOperation(
            Summary = "Lấy danh sách danh mục",
            Description = "Trả về danh sách tất cả danh mục trong hệ thống có hỗ trợ phân trang (Pagination)."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.GetAllAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo danh mục mới",
            Description = "Tạo mới một danh mục sản phẩm trong hệ thống."
        )]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.CreateCategoryAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("update/{id:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật danh mục",
            Description = "Cập nhật thông tin tên, mô tả hoặc trạng thái của danh mục theo ID."
        )]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("delete/{id:guid}")]
        [SwaggerOperation(
            Summary = "Xóa danh mục",
            Description = "Xóa (hoặc vô hiệu hóa) một danh mục khỏi hệ thống theo ID."
        )]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("get-by-id/{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin danh mục theo ID",
            Description = "Trả về chi tiết thông tin của một danh mục theo ID."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _categoryService.GetByIdAsync(id, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("active")]
        [SwaggerOperation(
            Summary = "Lấy danh sách danh mục đang hoạt động",
            Description = "Trả về danh sách danh mục đang hoạt động (IsActive = true) có hỗ trợ tìm kiếm."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetActive([FromQuery] GetActiveCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.GetActiveAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm danh mục",
            Description = "Tìm kiếm danh mục theo từ khóa tên với phân trang."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromQuery] CategorySearchRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryService.SearchAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
