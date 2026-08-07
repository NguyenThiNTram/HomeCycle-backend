using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public sealed class NegotiationProposalResponse
    {
        public Guid MessageId { get; set; }
        public Guid NegotiationId { get; set; }
        public Guid SenderId { get; set; }

        public decimal? OfferPrice { get; set; }
        public int OfferQuantity { get; set; }

        public MessageOfferStatus OfferStatus { get; set; }
        public NegotiationStatus NegotiationStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
