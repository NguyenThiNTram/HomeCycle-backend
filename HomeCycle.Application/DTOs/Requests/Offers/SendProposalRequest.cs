using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Offers
{
    public sealed class SendProposalRequest
    {
        public decimal OfferPrice { get; set; }
        public int OfferQuantity { get; set; }
        public string? MessageContent { get; set; }
    }
}
