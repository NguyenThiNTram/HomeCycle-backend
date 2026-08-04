using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Offers
{
    public class CounterInitialOfferRequest
    {
        public decimal OfferPrice { get; set; }

        public int OfferQuantity { get; set; }

        public string? MessageContent { get; set; }
    }
}
