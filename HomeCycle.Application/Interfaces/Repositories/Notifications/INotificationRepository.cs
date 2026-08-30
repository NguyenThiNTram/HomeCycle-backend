using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Notifications
{
    public interface INotificationRepository
    {
        Task AddAsync(notification notification, CancellationToken cancellationToken = default);

        Task<notification?> GetByIdAndUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

        Task<PagedResult<notification>> GetByUserAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
