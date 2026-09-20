using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using Microsoft.AspNetCore.SignalR;

namespace HomeCycle.API.Hubs
{
    public sealed class SignalRAppointmentRealtimePublisher : IAppointmentRealtimePublisher
    {
        private readonly IHubContext<ChatHub, IChatClient> _hubContext;

        public SignalRAppointmentRealtimePublisher(IHubContext<ChatHub, IChatClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishUpdatedAsync(IReadOnlyList<Guid> userIds, AppointmentUpdatedResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _hubContext.Clients
                .Users(userIds.Distinct().Select(x => x.ToString()))
                .AppointmentUpdated(response)
                .WaitAsync(cancellationToken);
        }
    }
}
