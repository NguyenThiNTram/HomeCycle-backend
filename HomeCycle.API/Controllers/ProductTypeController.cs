using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeCycle.API.Controllers
{
    [Route("api/product-types")]
    [ApiController]
    public class ProductTypeController : ControllerBase
    {
        private readonly IProductTypeService _productTypeService;

        public ProductTypeController(IProductTypeService productTypeService)
        {
            _productTypeService = productTypeService;
        }

        [HttpPost("create")]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status201Created)]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProductTypeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return ProcessErrorResult(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Data.ProductTypeId },result);
        }

        [HttpPut("update/{id:guid}")]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductTypeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.UpdateAsync(id, request, cancellationToken);
            return ProcessResult(result);
        }

        [HttpDelete("delete/{id:guid}")]
        //[ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _productTypeService.DeleteAsync(id, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("get-by-id/{id:guid}")]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(Result<ProductTypeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetByIdAsync(id, cancellationToken);
            return ProcessResult(result);
        }

        [HttpGet("get-all")]
        //[ProducesResponseType(typeof(Result<PagedResult<ProductTypeResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.GetAllAsync(request, cancellationToken);
            return ProcessResult(result);
        }

        //[HttpGet("category/{categoryId:guid}")]
        //[ProducesResponseType(typeof(Result<IEnumerable<ProductTypeResponse>>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetByCategoryId(
        //    [FromRoute] Guid categoryId,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await _productTypeService.GetByCategoryIdAsync(categoryId, cancellationToken);
        //    return ProcessResult(result);
        //}

        [HttpGet("search")]
        //[ProducesResponseType(typeof(Result<PagedResult<ProductTypeResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] ProductTypeSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _productTypeService.SearchAsync(request, cancellationToken);
            return ProcessResult(result);
        }

        #region Private Helper Methods

        /// <summary>
        /// Bộ lọc điều hướng xử lý đầu ra tự động dựa trên trạng thái của đối tượng Result.
        /// </summary>
        private IActionResult ProcessResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return ProcessErrorResult(result);
        }

        /// <summary>
        /// Phân tích mã lỗi nghiệp vụ chuyên biệt để ánh xạ chính xác về các HTTP Status Code.
        /// </summary>
        private IActionResult ProcessErrorResult<T>(Result<T> result)
        {
            // Giả định đối tượng Error trong hệ thống của bạn có thuộc tính Code hoặc Type
            if (result.Error != null)
            {
                var errorCode = result.Error.Code;

                if (errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                    return NotFound(result);

                if (errorCode.Contains("AlreadyExists", StringComparison.OrdinalIgnoreCase) ||
                    errorCode.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
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
