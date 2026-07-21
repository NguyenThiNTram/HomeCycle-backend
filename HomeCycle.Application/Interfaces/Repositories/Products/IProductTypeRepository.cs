using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IProductTypeRepository
    {
        Task AddAsync(product_type entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(product_type entity, CancellationToken cancellationToken);
        Task<product_type?> AggregateUpdateAsync(Guid productTypeId, CancellationToken cancellationToken = default);
        Task<product_type?> GetByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<product_type>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
        Task<bool> ExistsByNameAsync(Guid categoryId, string productTypeName, CancellationToken cancellationToken = default);
        Task<PagedResult<product_type>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<product_type>> SearchAsync(ProductTypeSearchRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid productTypeId, CancellationToken cancellationToken);
    }
}
