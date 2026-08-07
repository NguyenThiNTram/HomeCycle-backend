using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Negotiates
{
    public sealed class SendNegotiationCounterRequest
    {
        public decimal OfferPrice { get; set; }

        public int OfferQuantity { get; set; }
    }
}
