using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Offers
{
    public class CreateOfferRequest
    {
        public Guid PostId { get; set; }

        public decimal OfferPrice { get; set; }

        public int OfferQuantity { get; set; }
    }

    public class AcceptOfferRequest
    {
        public int? Version { get; set; }
    }

    public class UpdateOfferRequest
    {
        public decimal? OfferPrice { get; set; }

        public int? OfferQuantity { get; set; }
        public int? Version { get; set; }
    }
}
