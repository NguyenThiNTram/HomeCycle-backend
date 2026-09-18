using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Entitlements
{
    public sealed record EffectiveWithdrawalEntitlements(
        decimal? DailyAmountLimit,
        int? DailyCountLimit,
        Guid? SubscriptionId);
}
