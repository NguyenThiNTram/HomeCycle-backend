using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Appointments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Appointments
{
    public class RejectAppointmentRescheduleRequestValidator : AbstractValidator<RejectAppointmentRescheduleRequest>
    {
        public RejectAppointmentRescheduleRequestValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
