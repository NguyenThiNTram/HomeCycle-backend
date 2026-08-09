using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class AgreementActionResponse
    {
        public string Message { get; set; } = string.Empty;
        public Guid AgreementId { get; set; }
        public AgreementStatus AgreementStatus { get; set; }
        public bool SellerConfirmed { get; set; }
        public bool BuyerConfirmed { get; set; }
        public DateTime? SellerConfirmedAt { get; set; }
        public DateTime? BuyerConfirmedAt { get; set; }
    }
}
