using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Notifications;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly HomeCycleDbContext _db;

        public NotificationRepository(HomeCycleDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(notification notification, CancellationToken cancellationToken = default)
        {
            await _db.Notifications.AddAsync(
                notification.ToInfrastructure(),
                cancellationToken);
        }

        public async Task<notification?> GetByIdAndUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.NotificationId == notificationId &&
                         x.UserId == userId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<notification>> GetByUserAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Notifications
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NotificationId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<notification>
            {
                Items = entities
                    .Select(x => x.ToDomain())
                    .ToList(),

                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _db.Notifications.CountAsync(
                x => x.UserId == userId && !x.IsRead,
                cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
        {
            var affectedRows = await _db.Notifications
                .Where(x =>
                    x.NotificationId == notificationId &&
                    x.UserId == userId &&
                    !x.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRead, true),
                    cancellationToken);

            return affectedRows > 0;
        }

        public Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _db.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsRead, true),
                    cancellationToken);
        }
    }
}
