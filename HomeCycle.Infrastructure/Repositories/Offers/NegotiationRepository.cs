using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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

        public Task<bool> HasAgreementAsync(Guid negotiationId, CancellationToken cancellationToken = default) =>
            _db.Agreement_Forms.AnyAsync(a => a.NegotiationId == negotiationId, cancellationToken);

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
            EnsureActiveTransaction();

            var entity = await _db.Negotiations
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ""Negotiation""
                    WHERE ""NegotiationId"" = {negotiationId}
                    FOR UPDATE")
                        .Include(x => x.Offer)
                        .Include(x => x.Post)
                        .AsNoTracking()
                        .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<negotiation>> GetByConversationIdAsync(Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Negotiations
                .AsNoTracking()
                .Include(x => x.Offer)
                .Include(x => x.Post)
                .Include(x => x.Seller)
                .Include(x => x.Buyer)
                .Where(x => x.ConversationId == conversationId);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NegotiationId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<negotiation>
            {
                Items = entities
                    .Select(x => x.ToDomain())
                    .ToList(),

                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task<bool> ExistsActiveByPostAndParticipantsAsync(Guid postId, Guid sellerId, Guid buyerId, CancellationToken cancellationToken = default, Guid? buyPostId = null)
        {
            // Gate all trades for the same sell post and participants, regardless of BuyPostId.
            // Confirmed alone is insufficient: payment fulfillment must also have created an order.
            return _db.Negotiations
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PostId == postId &&
                        x.SellerId == sellerId &&
                        x.BuyerId == buyerId &&
                        (x.NegotiationStatus == null || x.NegotiationStatus != (int)NegotiationStatus.Cancelled) &&
                        !(x.Agreement_Form != null &&
                          x.Agreement_Form.AgreementStatus == (int)AgreementStatus.Confirmed &&
                          x.Agreement_Form.Order != null),
                    cancellationToken);
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

        //tái sử dụng đoạn guard clause
        private void EnsureActiveTransaction()
        {
            if (_db.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException(
                    "FOR UPDATE requires an active database transaction.");
            }
        }
    }
}
