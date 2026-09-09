using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Inspections
{
    public sealed class ScheduleInspectionCollectionRequest
    {
        public int ExpectedRevision { get; set; }
        public DateTimeOffset CollectionDate { get; set; }
        public string? PickupAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        //public PaymentType PaymentType { get; set; }
        public decimal? EstimatedShippingFee { get; set; }
        public GhnShippingInfo? GhnInfo { get; set; }
    }
}
