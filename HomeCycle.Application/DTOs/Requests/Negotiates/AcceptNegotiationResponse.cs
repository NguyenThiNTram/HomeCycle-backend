using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Negotiates
{
    public sealed class AcceptNegotiationResponse
    {
        public Guid NegotiationId { get; set; }
        public NegotiationStatus NegotiationStatus { get; set; }
        public MessageOfferStatus ProposalStatus { get; set; }
        public OfferResponse CurrentOffer { get; set; } = null!;
    }
}
