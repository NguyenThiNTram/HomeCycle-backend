using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.DTOs.Responses.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IProductAttributeService
    {
        Task<Result<ProductAttributeResponse>> CreateAsync(
            Guid productTypeId, CreateAttributeRequest request, CancellationToken cancellationToken = default);
        Task<Result<ProductAttributeResponse>> UpdateAsync(
            Guid attributeId, UpdateAttributeRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> DeleteAsync(Guid attributeId, CancellationToken cancellationToken = default);

        Task<Result<ProductAttributeResponse>> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<ProductAttributeResponse>>> GetByProductTypeAsync(
            Guid productTypeId, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<AttributeFilterOptionResponse>>> GetFilterableAttributesAsync(
            Guid productTypeId, CancellationToken cancellationToken = default);

    }
}
