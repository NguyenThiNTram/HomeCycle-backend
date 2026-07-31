using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum AgreementStatus
    {
        Pending = 0,
        Awaiting_Payment = 1,
        Confirmed = 2, 
        Cancelled = 3, 
        Expired = 4 
    }
}
