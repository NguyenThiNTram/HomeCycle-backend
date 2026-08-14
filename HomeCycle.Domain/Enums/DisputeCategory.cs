using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum DisputeCategory
    {
        NoShow = 1, ItemMismatch = 2, SellerNotShipped = 3,
        DamagedOrLost = 4, ItemNotReceived = 5, FraudOrScam = 6,
        AbusiveReview = 7, Other = 99
    }
}
