using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class ShippingFeePreviewResponse
    {
        public Guid NegotiationId { get; init; }
        public int ServiceTypeId { get; init; }
        public decimal EstimatedShippingFee { get; init; }
        public string Currency { get; init; } = "VND";

        public required ShippingFeeBreakdownResponse Breakdown { get; init; }
    }

    public sealed class ShippingFeeBreakdownResponse
    {
        public decimal ServiceFee { get; init; }
        public decimal InsuranceFee { get; init; }
        public decimal PickStationFee { get; init; }
        public decimal CouponValue { get; init; }
        public decimal R2sFee { get; init; }
        public decimal DocumentReturnFee { get; init; }
        public decimal DoubleCheckFee { get; init; }
        public decimal CodFee { get; init; }
        public decimal PickRemoteAreasFee { get; init; }
        public decimal DeliverRemoteAreasFee { get; init; }
        public decimal CodFailedFee { get; init; }
    }
}
