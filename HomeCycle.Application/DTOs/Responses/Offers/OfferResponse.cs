using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Offers
{
    public class OfferResponse
    {
        public Guid OfferId { get; set; }

        public Guid PostId { get; set; }

        public OfferParticipantResponse Sender { get; set; } = null!;
        public OfferParticipantResponse Receiver { get; set; } = null!;


        public decimal? OfferPrice { get; set; }

        public int OfferQuantity { get; set; }

        public OfferStatus? OfferStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class OfferListItem
    {
        public Guid OfferId { get; set; }
        public Guid PostId { get; set; }

        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }

        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverAvatarUrl { get; set; }

        public decimal? OfferPrice { get; set; }
        public int OfferQuantity { get; set; }
        public OfferStatus? OfferStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class OfferDetailResponse
    {
        public Guid OfferId { get; set; }
        public Guid PostId { get; set; }

        public OfferParticipantResponse Sender { get; set; } = null!;
        public OfferParticipantResponse Receiver { get; set; } = null!;

        public decimal? OfferPrice { get; set; }
        public int OfferQuantity { get; set; }
        public OfferStatus OfferStatus { get; set; }

        public Guid? NegotiationId { get; set; }

        public bool CanUpdate { get; set; }
        public bool CanCancel { get; set; }
        public bool CanAccept { get; set; }
        public bool CanReject { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class OfferParticipantResponse
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
