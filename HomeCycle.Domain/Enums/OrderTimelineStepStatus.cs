using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum OrderTimelineStepStatus
    {
        Upcoming = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }
}
