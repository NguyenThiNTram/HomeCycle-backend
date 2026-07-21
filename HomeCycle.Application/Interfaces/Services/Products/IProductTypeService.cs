using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IProductTypeService
    {
        Task<Result<ProductTypeResponse>> CreateAsync(CreateProductTypeRequest request, CancellationToken cancellationToken = default);

        Task<Result<ProductTypeResponse>> UpdateAsync(Guid productTypeId, UpdateProductTypeRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> DeleteAsync(Guid productTypeId, CancellationToken cancellationToken = default);

        Task<Result<ProductTypeDetailResponse>> GetByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<product_type>>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<ProductTypeResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<ProductTypeResponse>>> SearchAsync(ProductTypeSearchRequest request, CancellationToken cancellationToken = default);
    }
}
