using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class GhnQuoteSnapshotDto
    {
        public DateTimeOffset ExpectedDeliveryAt { get; init; }
    }

    public sealed record GhnFeeQuoteResponse(decimal ServiceFee, decimal TotalFee);

    public sealed class GhnFeeBreakdownSnapshotDto
    {
        public decimal ServiceFee { get; init; }
        public decimal InsuranceFee { get; init; }
        public decimal PickStationFee { get; init; }
        public decimal CouponValue { get; init; }
        public decimal ReturnFee { get; init; }
        public decimal RedeliveryFee { get; init; }
        public decimal CodFee { get; init; }
        public decimal PickupRemoteAreaFee { get; init; }
        public decimal DeliveryRemoteAreaFee { get; init; }
    }
}
