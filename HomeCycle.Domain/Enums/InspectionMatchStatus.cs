using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum InspectionMatchStatus
    {
        MatchesDescription = 1,
        MinorDifference = 2,
        SignificantDifference = 3,
        DoesNotMatch = 4
    }
}
