using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.SupplierMatching.Normalization;
using HomeCycle.Domain.Enums;
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

        public async Task<IReadOnlyList<string>> SearchModelNumbersAsync(
            Guid productTypeId,
            Guid brandId,
            string normalizedKeyword,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var candidateLimit = Math.Max(limit, 1) * 10;
            var candidates = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.ProductTypeId == productTypeId &&
                    x.BrandId == brandId &&
                    x.ModelNumber != null &&
                    x.Post.PostType == (int)PostType.Sell &&
                    x.Post.Status != (int)PostStatus.Deleted &&
                    PostgresPricingFunctions.RegexpReplace(
                        x.ModelNumber.ToLower(),
                        "[^a-z0-9]",
                        string.Empty,
                        "g").StartsWith(normalizedKeyword))
                .Select(x => x.ModelNumber!)
                .OrderBy(x => x)
                .Take(candidateLimit)
                .ToListAsync(cancellationToken);

            return candidates
                .Select(model => model.Trim())
                .Where(model => model.Length > 0)
                .GroupBy(model => ModelNumberNormalizer.Normalize(model))
                .Where(group => group.Key is not null)
                .Select(group => group
                    .OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
                    .First())
                .OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(limit, 1))
                .ToArray();
        }

        //Task<ProductResponse?> IProductRepository.GetDetailAsync(Guid productId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
