using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Application.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers
{
    [Route("api/product-types")]
    [ApiController]
    [Authorize]
    public class ProductTypeController : ControllerBase
    {
        private readonly IProductTypeService _productTypeService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeOptionService _productAttributeOptionService;

        public ProductTypeController(
            IProductTypeService productTypeService,
            IProductAttributeService productAttributeService,
            IProductAttributeOptionService productAttributeOptionService)
        {
            _productTypeService = productTypeService;
            _productAttributeService = productAttributeService;
            _productAttributeOptionService = productAttributeOptionService;
        }

        // ==================== ProductType — CRUD cơ bản (giữ nguyên) ====================

        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo ProductType mới",
            Description = "Tạo mới loại sản phẩm (ProductType) thuộc một danh mục (Category) cụ thể trong hệ thống."
        )]
        public async Task<IActionResult> Create([FromBody] CreateProductTypeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return ProcessErrorResult(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Data.ProductTypeId }, result);
        }

        [HttpPut("update/{id:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật ProductType",
            Description = "Cập nhật thông tin tên, mô tả hoặc trạng thái của loại sản phẩm theo ProductTypeId."
        )]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductTypeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.UpdateAsync(id, request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpDelete("delete/{id:guid}")]
        [SwaggerOperation(
            Summary = "Xóa ProductType",
            Description = "Xóa (hoặc vô hiệu hóa) loại sản phẩm khỏi hệ thống theo ProductTypeId."
        )]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _productTypeService.DeleteAsync(id, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("get-by-id/{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin loại sản phẩm theo ID",
            Description = "Trả về chi tiết thông tin cơ bản của một loại sản phẩm theo ProductTypeId."
        )]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetByIdAsync(id, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("get-all")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tất cả loại sản phẩm",
            Description = "Trả về danh sách loại sản phẩm trong hệ thống có hỗ trợ phân trang (Pagination)."
        )]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetAllAsync(request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("category/{categoryId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách loại sản phẩm theo danh mục",
            Description = "Lấy toàn bộ danh sách loại sản phẩm (ProductTypes) trực thuộc một danh mục cụ thể (CategoryId)."
        )]
        public async Task<IActionResult> GetByCategoryId(
            [FromRoute] Guid categoryId,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetByCategoryIdAsync(categoryId, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm loại sản phẩm",
            Description = "Tìm kiếm loại sản phẩm theo từ khóa tên hoặc các bộ lọc tùy chọn có phân trang."
        )]
        public async Task<IActionResult> Search(
            [FromQuery] ProductTypeSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.SearchAsync(request, cancellationToken);
            return ProcessResult(result);
        }

        //
        [HttpGet("{productTypeId:guid}/posting-schema")]
        [SwaggerOperation(
            Summary = "Lấy schema cấu hình Form động",
            Description = "Lấy schema cấu hình Form động cho bài đăng bán (Selling Post) hoặc tin thu mua (Buying Post)"
        )]
        public async Task<IActionResult> GetPostingSchema(
            [FromRoute] Guid productTypeId,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetPostingSchemaAsync(productTypeId, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        // ==================== ProductAttribute — Read (giữ nguyên) ====================

        [HttpGet("{productTypeId:guid}/filterable-attributes")]
        [SwaggerOperation(
            Summary = "Lấy danh sách thuộc tính hỗ trợ lọc",
            Description = "Lấy danh sách các thuộc tính sản phẩm được cấu hình làm bộ lọc động (IsFilterable = true) phục vụ chức năng Tìm kiếm & Lọc bài đăng."
        )]
        public async Task<IActionResult> GetFilterableAttributes(
            Guid productTypeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.GetFilterableAttributesAsync(productTypeId, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("{productTypeId:guid}/attributes")]
        [SwaggerOperation(
            Summary = "Lấy danh sách thuộc tính theo loại sản phẩm",
            Description = "Lấy tất cả các thuộc tính động (Attributes) được định nghĩa cho một ProductTypeId cụ thể."
        )]
        public async Task<IActionResult> GetAttributesByProductType(Guid productTypeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.GetByProductTypeAsync(productTypeId, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("attributes/get-by-id/{attributeId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết thuộc tính theo ID",
            Description = "Trả về chi tiết cấu hình của một thuộc tính sản phẩm theo AttributeId."
        )]
        public async Task<IActionResult> GetAttributeById(Guid attributeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.GetByIdAsync(attributeId, cancellationToken);
            return ProcessResult(result);
        }

        // ==================== ProductAttribute — CRUD lẻ (mới bổ sung) ====================

        [HttpPost("{productTypeId:guid}/attributes/create")]
        [SwaggerOperation(
            Summary = "Tạo thuộc tính mới cho loại sản phẩm",
            Description = "Thêm một thuộc tính động mới (tên, DataType, InputMode, IsRequired...) vào loại sản phẩm chỉ định."
        )]
        public async Task<IActionResult> CreateAttribute(
            Guid productTypeId, [FromBody] CreateAttributeRequest request, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.CreateAsync(productTypeId, request, cancellationToken);
            if (!result.IsSuccess)
                return ProcessErrorResult(result);

            return CreatedAtAction(nameof(GetAttributeById), new { attributeId = result.Data.AttributeId }, result);
        }

        [HttpPut("attributes/update/{attributeId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật thuộc tính sản phẩm",
            Description = "Cập nhật các thuộc tính cấu hình của một AttributeId đã tồn tại."
        )]
        public async Task<IActionResult> UpdateAttribute(
            Guid attributeId, [FromBody] UpdateAttributeRequest request, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.UpdateAsync(attributeId, request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpDelete("attributes/delete/{attributeId:guid}")]
        [SwaggerOperation(
            Summary = "Xóa thuộc tính sản phẩm",
            Description = "Xóa thuộc tính sản phẩm khỏi loại sản phẩm tương ứng theo AttributeId."
        )]
        public async Task<IActionResult> DeleteAttribute(Guid attributeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.DeleteAsync(attributeId, cancellationToken);
            return ProcessResult(result);
        }

        // ==================== ProductAttributeOption — CRUD lẻ (mới bổ sung) ====================

        [HttpGet("attributes/{attributeId:guid}/options")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tùy chọn của thuộc tính",
            Description = "Lấy danh sách tất cả giá trị lựa chọn (Options) thuộc về một AttributeId có kiểu dữ liệu danh mục/lựa chọn."
        )]
        public async Task<IActionResult> GetOptionsByAttribute(Guid attributeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeOptionService.GetByAttributeAsync(attributeId, cancellationToken);
            return ProcessResult(result);
        }

        [HttpPost("attributes/{attributeId:guid}/options/create")]
        [SwaggerOperation(
            Summary = "Tạo tùy chọn mới cho thuộc tính",
            Description = "Thêm một giá trị lựa chọn (Option) mới cho thuộc tính dạng danh sách/dropdown."
        )]
        public async Task<IActionResult> CreateOption(
            Guid attributeId, [FromBody] CreateAttributeOptionRequest request, CancellationToken cancellationToken)
        {
            var result = await _productAttributeOptionService.CreateAsync(attributeId, request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpPut("options/update/{optionId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật tùy chọn của thuộc tính",
            Description = "Cập nhật nhãn hoặc giá trị hiển thị của một OptionId."
        )]
        public async Task<IActionResult> UpdateOption(
            Guid optionId, [FromBody] UpdateAttributeOptionRequest request, CancellationToken cancellationToken)
        {
            var result = await _productAttributeOptionService.UpdateAsync(optionId, request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpDelete("options/delete/{optionId:guid}")]
        [SwaggerOperation(
            Summary = "Xóa tùy chọn của thuộc tính",
            Description = "Xóa một giá trị lựa chọn (Option) khỏi thuộc tính tương ứng theo OptionId."
        )]
        public async Task<IActionResult> DeleteOption(Guid optionId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeOptionService.DeleteAsync(optionId, cancellationToken);
            return ProcessResult(result);
        }

        #region Private Helper Methods

        private IActionResult ProcessResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return ProcessErrorResult(result);
        }

        private IActionResult ProcessErrorResult<T>(Result<T> result)
        {
            if (result.Error != null)
            {
                var errorCode = result.Error.Code;

                if (errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                    return NotFound(result);

                if (errorCode.Contains("AlreadyExists", StringComparison.OrdinalIgnoreCase) ||
                    errorCode.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
                    errorCode.Contains("InUse", StringComparison.OrdinalIgnoreCase))
                    return Conflict(result);

                if (errorCode.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
                    errorCode.Contains("Validation", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(result);
            }

            return BadRequest(result);
        }

        #endregion
    }
}