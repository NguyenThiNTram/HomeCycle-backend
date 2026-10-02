using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Appointments
{
    public interface IAppointmentReminderProcessor
    {
        Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}
