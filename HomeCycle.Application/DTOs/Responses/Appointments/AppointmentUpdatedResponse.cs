using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public sealed class AppointmentUpdatedResponse
    {
        public Guid AppointmentId { get; init; }
        public Guid AgreementId { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
