using HomeCycle.Application.DTOs.Responses.Appointments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Appointments
{
    public interface IAppointmentRealtimePublisher
    {
        Task PublishUpdatedAsync(IReadOnlyList<Guid> userIds, AppointmentUpdatedResponse response, CancellationToken cancellationToken = default);
    }
}
