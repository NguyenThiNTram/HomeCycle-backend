using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.PlatformPolicies
{
    public class UpdateOrderPolicyRequest
    {
        public int? BuyerReceiveConfirmationTimeoutHours { get; set; }
    }
}
