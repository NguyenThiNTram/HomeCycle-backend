using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface ICategoryRepository
    {
        Task AddAsync(category category, CancellationToken cancellationToken = default);
        Task UpdateAsync(category category, CancellationToken cancellationToken = default);

        Task<category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
        Task<category?> GetByNameAsync(string categoryName, CancellationToken cancellationToken = default);
        Task<PagedResult<category>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
        Task<PagedResult<category>> GetActiveAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);

        Task<PagedResult<category>> SearchAsync(string keyword, PaginationRequest pagination, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string categoryName, CancellationToken cancellationToken = default);
    }
}
