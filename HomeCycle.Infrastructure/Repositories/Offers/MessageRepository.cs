using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Responses.Messages;
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

        public async Task<PagedResult<message>> GetByConversationIdAsync(Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Messages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId);

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (request.PageNumber - 1) * request.PageSize;

            // Page 1 chứa nhóm tin mới nhất
            var entities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.MessageId)
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Trong từng page, trả về theo chiều cũ -> mới
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

        public async Task<int> MarkConversationAsReadAsync(Guid conversationId, Guid readerId, DateTime readAt, CancellationToken cancellationToken = default)
        {
            EnsureUtc(readAt);

            return await _db.Messages
                .Where(x =>
                    x.ConversationId == conversationId &&
                    x.SenderId != readerId &&
                    !x.IsRead &&
                    x.CreatedAt <= readAt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRead, true)
                        .SetProperty(x => x.UpdatedAt, readAt),
                    cancellationToken);
        }

        public Task<int> CountUnreadByConversationForUserAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return _db.Messages
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.ConversationId == conversationId &&
                        x.SenderId != userId &&
                        !x.IsRead,
                    cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetUnreadCountsByConversationsAsync(Guid userId, IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken = default)
        {
            if (conversationIds.Count == 0)
                return new Dictionary<Guid, int>();

            var distinctIds = conversationIds
                .Distinct()
                .ToList();

            var counts = await _db.Messages
                .AsNoTracking()
                .Where(x =>
                    x.ConversationId.HasValue &&
                    distinctIds.Contains(x.ConversationId.Value) &&
                    x.SenderId != userId &&
                    !x.IsRead)
                .GroupBy(x => x.ConversationId!.Value)
                .Select(group => new
                {
                    ConversationId = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            var result = distinctIds.ToDictionary(
                conversationId => conversationId,
                _ => 0);

            foreach (var item in counts)
            {
                result[item.ConversationId] = item.Count;
            }

            return result;
        }

        public async Task<Dictionary<Guid, message>> GetLatestByConversationsAsync(IReadOnlyCollection<Guid> conversationIds, CancellationToken cancellationToken = default)
        {
            if (conversationIds.Count == 0)
                return new Dictionary<Guid, message>();

            var distinctIds = conversationIds
                .Distinct()
                .ToList();

            var latestMessageIds = _db.Messages
                .AsNoTracking()
                .Where(x =>
                    x.ConversationId.HasValue &&
                    distinctIds.Contains(x.ConversationId.Value))
                .GroupBy(x => x.ConversationId)
                .Select(group => group
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.MessageId)
                    .Select(x => x.MessageId)
                    .First());

            var entities = await _db.Messages
                .AsNoTracking()
                .Where(x => latestMessageIds.Contains(x.MessageId))
                .ToListAsync(cancellationToken);

            return entities
                .Where(x => x.ConversationId.HasValue)
                .ToDictionary(
                    x => x.ConversationId!.Value,
                    x => x.ToDomain());
        }

        public async Task<message?> GetByClientMessageIdInConversationAsync(Guid conversationId, Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.ConversationId == conversationId &&
                        x.SenderId == senderId &&
                        x.ClientMessageId == clientMessageId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<Dictionary<Guid, int>> GetUnreadCountsByConversationAsync(Guid conversationId, Guid userOneId, Guid userTwoId, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, int>
            {
                [userOneId] = 0,
                [userTwoId] = 0
            };

            var counts = await _db.Messages
                .AsNoTracking()
                .Where(x =>
                    x.ConversationId == conversationId &&
                    !x.IsRead)
                .GroupBy(x => x.SenderId)
                .Select(group => new
                {
                    SenderId = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            foreach (var item in counts)
            {
                // Tin do user one gửi là unread của user two.
                if (item.SenderId == userOneId)
                    result[userTwoId] += item.Count;
                else if (item.SenderId == userTwoId)
                    result[userOneId] += item.Count;
            }

            return result;
        }

        public async Task<Dictionary<Guid, UnreadCountResult>> GetUnreadCountsDetailAsync(Guid conversationId, Guid userOneId, Guid userTwoId, CancellationToken cancellationToken = default)
        {
            // Khởi tạo khung kết quả cho cả 2 User
            var result = new Dictionary<Guid, UnreadCountResult>
            {
                [userOneId] = new UnreadCountResult(),
                [userTwoId] = new UnreadCountResult()
            };

            // Lấy danh sách tin nhắn chưa đọc grouped theo SenderId và NegotiationId
            var rawCounts = await _db.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId && !m.IsRead)
                .GroupBy(m => new { m.SenderId, m.NegotiationId })
                .Select(g => new
                {
                    SenderId = g.Key.SenderId,
                    NegotiationId = g.Key.NegotiationId,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            foreach (var item in rawCounts)
            {
                // Tin do userOne gửi -> tính vào Unread của userTwo
                // Tin do userTwo gửi -> tính vào Unread của userOne
                var recipientId = (item.SenderId == userOneId) ? userTwoId : userOneId;

                // Cộng dồn tổng cho Conversation
                result[recipientId].TotalConversationUnread += item.Count;

                // Ghi nhận chi tiết cho Negotiation (nếu có NegotiationId)
                if (item.NegotiationId.HasValue)
                {
                    var negId = item.NegotiationId.Value;
                    if (!result[recipientId].UnreadByNegotiation.ContainsKey(negId))
                    {
                        result[recipientId].UnreadByNegotiation[negId] = 0;
                    }
                    result[recipientId].UnreadByNegotiation[negId] += item.Count;
                }
            }

            return result;
        }

        private static void EnsureUtc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "Thời gian đọc tin nhắn phải sử dụng UTC.",
                    nameof(value));
            }
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
