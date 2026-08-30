using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Appointments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Appointments
{
    public class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
    {
        public CancelAppointmentRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .MaximumLength(500);
        }
    }
}
