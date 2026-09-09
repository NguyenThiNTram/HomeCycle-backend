using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.PlatformPolicies
{
    public class UpdatePaymentPolicyRequest
    {
        public decimal? DepositRatePercent { get; set; }

        public int? PaymentExpiryMinutes { get; set; }
    }
}
