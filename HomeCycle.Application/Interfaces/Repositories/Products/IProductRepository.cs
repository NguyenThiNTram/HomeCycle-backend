using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Products
{
    public interface IProductRepository
    {
        Task AddAsync(product entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(product entity, CancellationToken cancellationToken = default);

        Task<product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<product?> GetDetailAsync(Guid productId, CancellationToken cancellationToken = default);

        Task<product?> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<product?> GetDetailByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<bool> ExistsByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
    }
}
