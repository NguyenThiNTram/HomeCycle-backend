using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Repositories.Notifications;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalRNotificationRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishCreatedAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients
                .User(userId.ToString("D"))
                .NotificationCreated(notification)
                .WaitAsync(cancellationToken);
        }

        public Task PublishReadAsync(Guid userId, NotificationReadResponse response, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients
                .User(userId.ToString("D"))
                .NotificationRead(response)
                .WaitAsync(cancellationToken);
        }

        public Task PublishAllReadAsync(Guid userId, NotificationsReadAllResponse response, CancellationToken cancellationToken = default)
        {
            return _hubContext.Clients
                .User(userId.ToString("D"))
                .NotificationsReadAll(response)
                .WaitAsync(cancellationToken);
        }
    }
}
