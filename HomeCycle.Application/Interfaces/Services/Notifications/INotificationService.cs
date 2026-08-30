using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Notifications
{
    public interface INotificationService
    {
        Task<notification> AddPendingAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default);

        // Chỉ gọi sau khi transaction đã commit
        Task PublishCreatedSafelyAsync(notification notification);

        Task<Result<PagedResult<NotificationResponse>>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<UnreadNotificationCountResponse>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<NotificationReadResponse>> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

        Task<Result<NotificationsReadAllResponse>> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
