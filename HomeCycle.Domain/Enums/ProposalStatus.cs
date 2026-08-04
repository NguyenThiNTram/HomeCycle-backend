using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum ProposalStatus
    {
        // Enum này được lưu trong Messages.OfferStatus.
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Superseded = 3,
        Withdrawn = 4,
        Cancelled = 5
    }
}
