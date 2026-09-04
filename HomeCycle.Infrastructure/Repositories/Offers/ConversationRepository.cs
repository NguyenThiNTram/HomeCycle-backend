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
    public class ConversationRepository : IConversationRepository
    {
        private readonly HomeCycleDbContext _db;

        public ConversationRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Conversations
                .AsNoTracking()
                .Include(x => x.UserOne)
                .Include(x => x.UserTwo)
                .FirstOrDefaultAsync(
                    x => x.ConversationId == conversationId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<conversation?> GetByUsersAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
        {
            ValidateUserPair(firstUserId, secondUserId);

            var entity = await _db.Conversations
                .AsNoTracking()
                .Include(x => x.UserOne)
                .Include(x => x.UserTwo)
                .SingleOrDefaultAsync(
                    x =>
                        (x.UserOneId == firstUserId &&
                         x.UserTwoId == secondUserId) ||
                        (x.UserOneId == secondUserId &&
                         x.UserTwoId == firstUserId),
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<conversation> GetOrCreateAsync(Guid firstUserId, Guid secondUserId, DateTime activityAt, CancellationToken cancellationToken = default)
        {
            ValidateUserPair(firstUserId, secondUserId);
            EnsureActiveTransaction();
            EnsureUtc(activityAt);

            var conversationId = Guid.NewGuid();

            // ON CONFLICT xử lý hai request đồng thời cùng tạo một Conversation
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO public."Conversation"
                (
                    "ConversationId",
                    "UserOneId",
                    "UserTwoId",
                    "LastActivityAt",
                    "CreatedAt"
                )
                VALUES
                (
                    {conversationId},
                    LEAST({firstUserId}, {secondUserId}),
                    GREATEST({firstUserId}, {secondUserId}),
                    {activityAt},
                    {activityAt}
                )
                ON CONFLICT ("UserOneId", "UserTwoId")
                DO NOTHING;
                """,
                cancellationToken);

            var conversation = await GetByUsersAsync(firstUserId, secondUserId, cancellationToken);

            if (conversation is null)
                throw new InvalidOperationException("Không thể tạo hoặc lấy Conversation của cặp người dùng.");

            return conversation;
        }

        public async Task<PagedResult<conversation>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Conversations
                .AsNoTracking()
                .Include(x => x.UserOne)
                .Include(x => x.UserTwo)
                .Where(x =>
                    x.UserOneId == userId ||
                    x.UserTwoId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .OrderByDescending(x => x.LastActivityAt)
                .ThenByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.ConversationId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<conversation>
            {
                Items = entities
                    .Select(x => x.ToDomain()!)
                    .ToList(),

                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return _db.Conversations
                .AsNoTracking()
                .AnyAsync(x => x.ConversationId == conversationId && (x.UserOneId == userId || x.UserTwoId == userId),
                    cancellationToken);
        }

        public async Task UpdateLastActivityAsync(Guid conversationId, DateTime activityAt, CancellationToken cancellationToken = default)
        {
            EnsureUtc(activityAt);

            // ngăn request late ghi đè LastActivityAt
            await _db.Conversations
                .Where(x =>
                    x.ConversationId == conversationId &&
                    x.LastActivityAt < activityAt)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.LastActivityAt,
                        activityAt),
                    cancellationToken);
        }

        private void EnsureActiveTransaction()
        {
            if (_db.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException("GetOrCreateAsync requires an active database transaction.");
            }
        }

        private static void ValidateUserPair(Guid firstUserId, Guid secondUserId)
        {
            if (firstUserId == Guid.Empty)
                throw new ArgumentException("First user ID không hợp lệ.", nameof(firstUserId));

            if (secondUserId == Guid.Empty)
                throw new ArgumentException("Second user ID không hợp lệ.", nameof(secondUserId));

            if (firstUserId == secondUserId)
                throw new ArgumentException("Conversation phải gồm hai người dùng khác nhau.");
        }

        private static void EnsureUtc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Thời gian Conversation phải sử dụng UTC.");
            }
        }

    }
}
