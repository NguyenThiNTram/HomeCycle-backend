using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public class NegotiationAccess
    {
        public static bool IsParticipant(negotiation negotiation, Guid userId)
        {
            return negotiation.BuyerId == userId ||
                   negotiation.SellerId == userId;
        }
    }
}
