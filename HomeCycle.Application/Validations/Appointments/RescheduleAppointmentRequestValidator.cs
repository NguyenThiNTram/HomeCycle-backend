using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.DTOs.Requests.Appointments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Appointments
{
    public class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
    {
        public RescheduleAppointmentRequestValidator()
        {
            RuleFor(x => x.ProposedAt)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("Thời gian đề xuất không hợp lệ.")
                .Must(x =>
                    x == default ||
                    AppointmentScheduleHelper.IsWithinBusinessHours(x))
                .WithMessage(AppointmentErrors.ScheduleOutsideBusinessHours.Message);
        }
    }
}
