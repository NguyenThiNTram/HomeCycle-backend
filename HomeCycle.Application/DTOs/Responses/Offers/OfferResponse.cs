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

        public Guid SenderId { get; set; }

        public Guid ReceiverId { get; set; }

        public decimal? OfferPrice { get; set; }

        public int OfferQuantity { get; set; }

        public int? OfferStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
