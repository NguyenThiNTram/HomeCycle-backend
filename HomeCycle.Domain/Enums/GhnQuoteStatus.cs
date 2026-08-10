using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum GhnQuoteStatus
    {
        NotCalculated = 0,
        Valid = 1,
        Stale = 2,
        Failed = 3
    }
}
