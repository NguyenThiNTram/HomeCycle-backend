using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum FinanceEventType
    {
        OrderPaymentCompleted = 1,
        OrderPaymentFailed = 2,
        OrderPaymentCancelled = 3,
        OrderPaymentExpired = 4,

        OrderRefunded = 5,
        OrderPayoutReleased = 6,

        SubscriptionPaymentCompleted = 7,
        SubscriptionPaymentFailed = 8,
        SubscriptionPaymentCancelled = 9,
        SubscriptionPaymentExpired = 10,

        WithdrawalRequested = 11,
        WithdrawalApproved = 12,
        WithdrawalCompleted = 13,
        WithdrawalRejected = 14,
        WithdrawalFailed = 15,
        OrderPaymentInitiated = 16,
        SubscriptionPaymentInitiated = 17
    }
}
