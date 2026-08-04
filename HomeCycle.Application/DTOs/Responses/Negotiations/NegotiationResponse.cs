using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public sealed class NegotiationResponse
    {
        public Guid OfferId { get; set; }
        public Guid NegotiationId { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public decimal? CurrentOfferPrice { get; set; }
        public int CurrentOfferQuantity { get; set; }
    }
}
