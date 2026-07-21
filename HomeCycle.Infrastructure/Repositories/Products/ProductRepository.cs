using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Products
{
    public class ProductRepository : IProductRepository
    {
        private readonly HomeCycleDbContext _db;

        public ProductRepository(HomeCycleDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(product entity, CancellationToken cancellationToken = default)
        {
            var infra = entity.ToInfrastructure();
            await _db.Products.AddAsync(infra, cancellationToken);
        }

        public async Task UpdateAsync(product entity, CancellationToken cancellationToken = default)
        {
            var infra = entity.ToInfrastructure();

            var localEntry = _db.Products.Local
                .FirstOrDefault(x => x.ProductId == infra.ProductId);

            if (localEntry != null)
                _db.Entry(localEntry).State = EntityState.Detached;

            _db.Products.Update(infra);
            await Task.CompletedTask;
        }

        public async Task<product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<product?> GetDetailAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductType)
                .Include(x => x.Brand)
                .Include(x => x.Product_Attribute_Values)
                    .ThenInclude(av => av.Attribute)
                .Include(x => x.Product_Attribute_Values)
                    .ThenInclude(av => av.Option)
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId,
                    cancellationToken);

            // Chuyển đổi từ Infrastructure.Product sang Domain.product
            return entity?.ToDomain();
        }

        public async Task<product?> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.PostId == postId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<product?> GetDetailByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Products
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.ProductType)
                .Include(x => x.Brand)
                .Include(x => x.Product_Attribute_Values).ThenInclude(av => av.Attribute)
                .Include(x => x.Product_Attribute_Values).ThenInclude(av => av.Option)
                .FirstOrDefaultAsync(
                    x => x.PostId == postId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            return await _db.Products.AnyAsync(
                x => x.PostId == postId, cancellationToken);
        }

        //Task<ProductResponse?> IProductRepository.GetDetailAsync(Guid productId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
