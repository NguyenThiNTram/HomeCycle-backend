using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IProductAttributeOptionService
    {
        Task<Result<ProductAttributeOptionResponse>> CreateAsync(
            Guid attributeId, CreateAttributeOptionRequest request, CancellationToken cancellationToken = default);

        Task<Result<ProductAttributeOptionResponse>> UpdateAsync(
            Guid optionId, UpdateAttributeOptionRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> DeleteAsync(Guid optionId, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<ProductAttributeOptionResponse>>> GetByAttributeAsync(
            Guid attributeId, CancellationToken cancellationToken = default);

    }
}
