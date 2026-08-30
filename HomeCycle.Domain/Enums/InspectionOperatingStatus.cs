using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum InspectionOperatingStatus
    {
        WorkingWell = 1,
        WorkingWithMinorIssue = 2,
        Unstable = 3,
        NotWorking = 4,
        UnableToTest = 5
    }
}
