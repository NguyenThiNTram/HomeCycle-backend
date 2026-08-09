using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Offers
{
    public class NegotiationRepository : INegotiationRepository
    {
        private readonly HomeCycleDbContext _db;

        public NegotiationRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<negotiation?> GetByOfferIdAsync(Guid offerId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Negotiations
                .AsNoTracking()
                .Include(x => x.Offer)
                .Include(x => x.Post)
                .FirstOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<negotiation?> GetByIdAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Negotiations
                .AsNoTracking()
                .Include(x => x.Offer)
                .Include(x => x.Post)
                .FirstOrDefaultAsync(x => x.NegotiationId == negotiationId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<negotiation?> GetByIdForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            //FOR UPDATE để khóa dòng dữ liệu trong Postgres.
            //AsNoTracking: tránh xung đột ChangeTracker với UpdateAsync.
            var entity = await _db.Negotiations
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ""Negotiation""
                    WHERE ""NegotiationId"" = {negotiationId}
                    FOR UPDATE")
                        .Include(x => x.Offer) // Bổ sung Include Offer
                        .Include(x => x.Post)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(negotiation entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Negotiations.AddAsync(infraEntity, cancellationToken);
        }

        public async Task<PagedResult<negotiation>> GetByParticipantAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Negotiations
                .AsNoTracking()
                .Include(n => n.Offer)
                .Include(n => n.Post)
                .Include(n => n.Seller)
                .Include(n => n.Buyer)
                .Where(n => n.BuyerId == userId || n.SellerId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .OrderByDescending(n => n.LastMessageAt != null ? n.LastMessageAt : n.CreatedAt)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<negotiation>
            {
                Items = entities.Select(n => n.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task UpdateAsync(negotiation entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            _db.Negotiations.Update(infraEntity);
            return Task.CompletedTask;
        }
    }
}
