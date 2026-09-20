using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    public sealed class SignalRCartRealtimePublisher : ICartRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalRCartRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishUpdatedAsync(Guid userId, CartUpdatedResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .User(userId.ToString())
                .CartUpdated(response)
                .WaitAsync(cancellationToken);
        }
    }
}
