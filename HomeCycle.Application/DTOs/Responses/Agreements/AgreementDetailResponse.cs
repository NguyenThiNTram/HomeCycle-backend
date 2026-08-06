using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class AgreementDetailResponse
    {
        public Guid AgreementId { get; set; }
        public Guid NegotiationId { get; set; }
        public Guid PostId { get; set; }
        public Guid SellerId { get; set; }
        public Guid BuyerId { get; set; }

        public decimal InitialPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public int Quantity { get; set; }

        public AgreementType AgreementType { get; set; }
        public PaymentType PaymentType { get; set; }
        public AgreementStatus AgreementStatus { get; set; }

        public AgreementDetailsDto? AgreementDetails { get; set; }

        public DateTime? BuyerConfirmedAt { get; set; }
        public DateTime? SellerConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
