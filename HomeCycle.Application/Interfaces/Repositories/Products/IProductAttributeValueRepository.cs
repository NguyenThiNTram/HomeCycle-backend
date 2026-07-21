using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IProductAttributeValueRepository
    {
        Task AddRangeAsync(IEnumerable<product_attribute_value> entities, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<product_attribute_value>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task RemoveByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}
