using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public sealed class PaymentQuoteResponseDto
    {
        public Guid AgreementId { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal DepositRatePercent { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal AmountToPay { get; set; }
    }
}
