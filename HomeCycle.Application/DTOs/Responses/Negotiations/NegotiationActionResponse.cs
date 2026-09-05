using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public class NegotiationActionResponse
    {
        public Guid NegotiationId { get; set; }
        public Guid? ConversationId { get; set; }
        public Guid OfferId { get; set; }

        public NegotiationStatus NegotiationStatus { get; set; }
        public OfferStatus OfferStatus { get; set; }

        public decimal? CurrentOfferPrice { get; set; }
        public int CurrentOfferQuantity { get; set; }
        public int? CurrentOfferVersion { get; set; }

        public NegotiationActionProposalResponse? Proposal { get; set; }

        public SystemMessageResponse SystemMessage { get; set; } = null!;
    }

    public sealed class NegotiationActionProposalResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }

        public decimal? OfferPrice { get; set; }
        public int OfferQuantity { get; set; }
        public MessageOfferStatus OfferStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class SystemMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }

        public MessageType MessageType { get; set; }
        public string MessageContent { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
