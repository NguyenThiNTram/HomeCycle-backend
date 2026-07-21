using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum BusinessOnboardingStatus
    {
        MissingProfile = 0,
        PendingApproval = 1,
        Rejected = 2,
        SurveyPending = 3,
        Completed = 4
    }
}
