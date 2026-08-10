using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Carts
{
    public interface ICartItemRepository
    {
        Task<List<cart_item>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<cart_item?> GetByIdAsync(Guid cartItemId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);

        Task AddAsync(cart_item entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid cartItemId, CancellationToken cancellationToken = default);
    }
}
