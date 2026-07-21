using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IProductAttributeRepository
    {
        //Task UpdateAsync(product_attribute entity, CancellationToken cancellationToken = default);
        Task AddAsync(product_attribute entity, CancellationToken cancellationToken = default);
        Task<product_attribute?> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<product_attribute>> GetByProductTypeAsync(Guid productTypeId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(Guid productTypeId, string attributeName, CancellationToken cancellationToken = default);
    }
}
