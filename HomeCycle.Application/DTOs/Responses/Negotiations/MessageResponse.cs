using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Negotiations
{
    public class MessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid NegotiationId { get; set; }
        public Guid SenderId { get; set; }

        public string? MessageContent { get; set; }
        public int MessageType { get; set; }

        public decimal? OfferPrice { get; set; }
        public int? OfferQuantity { get; set; }
        public int? OfferStatus { get; set; }

        public string? MediaUrl { get; set; }
        public decimal? BasePriceSnapshot { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}