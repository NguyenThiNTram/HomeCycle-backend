using HomeCycle.Application.DTOs.Responses.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Notifications
{
    public interface INotificationRealtimePublisher
    {
        Task PublishCreatedAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default);

        Task PublishReadAsync(Guid userId, NotificationReadResponse response, CancellationToken cancellationToken = default);

        Task PublishAllReadAsync(Guid userId, NotificationsReadAllResponse response, CancellationToken cancellationToken = default);
    }
}
