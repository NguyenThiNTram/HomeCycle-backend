using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public sealed class PaymentStatusResponseDto
    {
        public PaymentStatus PaymentStatus { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? AppointmentId { get; set; }
    }
}
