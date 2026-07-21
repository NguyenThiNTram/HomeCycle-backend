using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IProductAttributeOptionRepository
    {
        //Task UpdateAsync(product_attribute_option entity, CancellationToken cancellationToken = default);

        Task AddAsync(product_attribute_option entity, CancellationToken cancellationToken = default);
        Task<product_attribute_option?> GetByIdAsync(Guid optionId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<product_attribute_option>> GetByAttributeAsync(Guid attributeId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid attributeId, string optionValue, CancellationToken cancellationToken = default);
    }
}
