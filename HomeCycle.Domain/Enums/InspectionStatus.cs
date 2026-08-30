using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum InspectionStatus
    {
        Draft = 0,
        PendingSellerConfirmation = 1,
        Accepted = 2,
        Rejected = 3
    }
}
