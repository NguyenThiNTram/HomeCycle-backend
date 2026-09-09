using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    public sealed class SignalROrderTrackingRealtimePublisher : IOrderTrackingRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalROrderTrackingRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishUpdatedAsync(
            Guid orderId,
            OrderTrackingUpdatedResponse response,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Group(ChatGroupName.ForOrder(orderId))
                .OrderTrackingUpdated(response)
                .WaitAsync(cancellationToken);
        }
    }
}
