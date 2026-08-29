using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using MathNet.Numerics.Distributions;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
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

        public async Task AddAsync(message entity, CancellationToken cancellationToken = default)
        {
            var infrastructureEntity = entity.ToInfrastructure();
            await _db.Messages.AddAsync(infrastructureEntity, cancellationToken);
        }

        public async Task<message?> GetByClientMessageIdAsync(Guid negotiationId, Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.NegotiationId == negotiationId &&
                    x.SenderId == senderId &&
                    x.ClientMessageId == clientMessageId,
                cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Messages
           .AsNoTracking()
           .FirstOrDefaultAsync(
               x => x.MessageId == messageId,
               cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<message?> GetByIdForUpdateAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            EnsureActiveTransaction();

            var entity = await _db.Messages
                .FromSqlInterpolated($"""
                SELECT *
                FROM "Messages"
                WHERE "MessageId" = {messageId}
                FOR UPDATE
                """)
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

            // Lấy từ mới đến cũ để page 1 luôn là nhóm tin mới nhất.
            var entities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.MessageId)
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Đảo lại để FE hiển thị trong mỗi trang theo thứ tự cũ đến mới.
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

        public async Task<message?> GetPendingProposalByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var pendingStatus = (int)MessageOfferStatus.Pending;
            var offerType = (int)MessageType.Offer;
            var counterOfferType = (int)MessageType.CounterOffer;

            var entity = await _db.Messages
                .AsNoTracking()
                .Where(x =>
                    x.NegotiationId == negotiationId &&
                    x.OfferStatus == pendingStatus &&
                    (
                        x.MessageType == offerType ||
                        x.MessageType == counterOfferType
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.MessageId)
                .FirstOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<message?> GetPendingProposalForUpdateAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            EnsureActiveTransaction();

            var pendingStatus = (int)MessageOfferStatus.Pending;
            var offerType = (int)MessageType.Offer;
            var counterOfferType = (int)MessageType.CounterOffer;

            var entity = await _db.Messages
                .FromSqlInterpolated($"""
                SELECT *
                FROM "Messages"
                WHERE "NegotiationId" = {negotiationId}
                  AND "MessageType" IN ({offerType}, {counterOfferType})
                  AND "OfferStatus" = {pendingStatus}
                ORDER BY "CreatedAt" DESC, "MessageId" DESC
                LIMIT 1
                FOR UPDATE
                """)
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        //Người gọi là một trong hai người thuộc message
        //Message đúng loại proposal
        //Trạng thái hiện tại vẫn bằng expectedStatus
        //Chỉ một request được chuyển trạng thái thành công
        public async Task<bool> TryUpdateProposalStatusAsync(Guid messageId, MessageOfferStatus expectedStatus, MessageOfferStatus newStatus, DateTime updatedAt, CancellationToken cancellationToken = default)
        {
            var offerType = (int)MessageType.Offer;
            var counterOfferType = (int)MessageType.CounterOffer;
            var now = updatedAt;

            var affectedRows = await _db.Messages
                .Where(x =>
                    x.MessageId == messageId &&
                    (x.MessageType == offerType ||
                     x.MessageType == counterOfferType) &&
                    x.OfferStatus == (int)expectedStatus)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.OfferStatus, (int)newStatus)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);

            return affectedRows == 1;
        }

        public async Task<int> MarkAsReadAsync(Guid negotiationId, Guid readerId, DateTime readAt, CancellationToken cancellationToken = default)
        {
            return await _db.Messages
                .Where(x =>
                    x.NegotiationId == negotiationId &&
                    x.SenderId != readerId &&
                    !x.IsRead &&
                    x.CreatedAt <= readAt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRead, true)
                        .SetProperty(x => x.UpdatedAt, readAt),
                    cancellationToken);
        }

        public async Task<int> CountUnreadByNegotiationForUserAsync(Guid negotiationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Messages
                .AsNoTracking()
                .CountAsync(
                    m => m.NegotiationId == negotiationId &&
                         m.SenderId != userId &&
                         !m.IsRead,
                    cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetUnreadCountsByNegotiationAsync(Guid negotiationId, Guid buyerId, Guid sellerId, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, int>
            {
                [buyerId] = 0,
                [sellerId] = 0
            };

            var counts = await _db.Messages
                .AsNoTracking()
                .Where(m => m.NegotiationId == negotiationId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var c in counts)
            {
                if (c.SenderId == buyerId) result[sellerId] += c.Count;
                else if (c.SenderId == sellerId) result[buyerId] += c.Count;
            }

            return result;
        }

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
