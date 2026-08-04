using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IBrandRepository
    {
        Task<brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(string brandName, CancellationToken cancellationToken = default);

        Task AddAsync(brand brand, CancellationToken cancellationToken = default);

        Task UpdateAsync(brand brand, CancellationToken cancellationToken = default);

        Task<PagedResult<brand>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<brand>> SearchAsync(string keyword, BrandSearchRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<brand>> GetActiveAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    }
}
