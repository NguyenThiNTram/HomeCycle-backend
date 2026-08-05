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
    public class MessageRepository : IMessageRepository
    {
        private readonly HomeCycleDbContext _db;

        public MessageRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<message?> GetPendingProposalByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var pendingStatus = (int)MessageOfferStatus.Pending;
            var initialOfferType = (int)MessageType.Offer;
            var counterOfferType = (int)MessageType.CounterOffer;

            var entity = await _db.Messages
                .AsNoTracking()
                .Where(x =>
                    x.NegotiationId == negotiationId &&
                    x.OfferStatus == pendingStatus &&
                    (
                        x.MessageType == initialOfferType ||
                        x.MessageType == counterOfferType
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<message?> GetPendingProposalForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            //Đồng bộ predicate với GetPendingProposalByNegotiationAsync:
            //proposal Pending có thể là Offer ban đầu (Accept) hoặc CounterOffer.
            //FOR UPDATE khóa dòng để serialize các counter/accept cùng lúc trên cùng negotiation.
            //AsNoTracking: tránh xung đột ChangeTracker với UpdateAsync.
            var offerType = (int)MessageType.Offer;
            var counterOfferType = (int)MessageType.CounterOffer;
            var pending = (int)ProposalStatus.Pending;

            var entity = await _db.Messages
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ""Messages""
                    WHERE ""NegotiationId"" = {negotiationId}
                      AND ""MessageType"" IN ({offerType}, {counterOfferType})
                      AND ""OfferStatus"" = {pending}
                    ORDER BY ""CreatedAt"" DESC
                    LIMIT 1
                    FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<message?> GetByIdForUpdateAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            //AsNoTracking: tránh xung đột ChangeTracker với UpdateAsync.
            var entity = await _db.Messages
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM ""Messages""
                    WHERE ""MessageId"" = {messageId}
                    FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<message>> GetByNegotiationIdAsync(Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Messages
                .AsNoTracking()
                .Where(x => x.NegotiationId == negotiationId);

            var totalCount = await query.CountAsync(cancellationToken);

            var skip = (request.PageNumber - 1) * request.PageSize;

            var entities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.MessageId)
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = entities
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.MessageId)
                .Select(x => x.ToDomain())
                .ToList();

            return new PagedResult<message>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task AddAsync(message entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Messages.AddAsync(infraEntity, cancellationToken);
        }

        public Task UpdateAsync(message entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            _db.Messages.Update(infraEntity);
            return Task.CompletedTask;
        }
    }
}
