using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Inspections
{
    public sealed class ScheduleInspectionCollectionResponse
    {
        public bool IsPreview { get; set; }
        public string? PreviewToken { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? ExpectedDeliveryAt { get; set; }
        public Guid InspectionFormId { get; set; }
        public int Revision { get; set; }
        public Guid OrderId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid CollectionAppointmentId { get; set; }
        public Guid ShipmentId { get; set; }
        //public Guid? PaymentId { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        //public PaymentType PaymentType { get; set; }
        public decimal EstimatedShippingFee { get; set; }
        //public decimal AdditionalPaymentAmount { get; set; }
        //public bool PaymentRequired { get; set; }
        public DateTime CollectionDate { get; set; }
    }
}
