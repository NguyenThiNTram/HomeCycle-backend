using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Carts
{
    public class CartRepository : ICartItemRepository
    {
        private readonly HomeCycleDbContext _db;

        public CartRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<List<cart_item>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var entities = await _db.Cart_Items
                .AsNoTracking()
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.ProductType)
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Brand)
                .Include(x => x.Post)
                    .ThenInclude(x => x.User)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<cart_item?> GetByIdAsync(Guid cartItemId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Cart_Items
                .AsNoTracking()
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.ProductType)
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Category)
                .Include(x => x.Post)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.Brand)
                .Include(x => x.Post)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
        {
            return await _db.Cart_Items.AnyAsync(
                x => x.UserId == userId && x.PostId == postId,
                cancellationToken);
        }

        public async Task AddAsync(cart_item entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Cart_Items.AddAsync(infraEntity, cancellationToken);
        }

        public async Task DeleteAsync(Guid cartItemId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Cart_Items
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId, cancellationToken);

            if (entity != null)
            {
                _db.Cart_Items.Remove(entity);
            }
        }
    }
}
