using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Posts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IProductAttributeService
    {
        Task<Result<IReadOnlyList<AttributeFilterOptionResponse>>> GetFilterableAttributesAsync(
            Guid productTypeId, CancellationToken cancellationToken = default);
    }
}
