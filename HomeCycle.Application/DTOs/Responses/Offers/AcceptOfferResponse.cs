using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Offers
{
    public class AcceptOfferResponse
    {
        public Guid OfferId { get; set; }
        public Guid NegotiationId { get; set; }
        public OfferStatus OfferStatus { get; set; }
        public NegotiationStatus NegotiationStatus { get; set; }
    }
}
