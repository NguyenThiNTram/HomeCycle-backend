using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Configs
{
    public class WithdrawalPolicyConfigDto
    {
        public decimal MinimumWithdrawalAmount { get; set; }
        public decimal MaximumWithdrawalAmount { get; set; }
        public decimal DailyWithdrawalLimit { get; set; }
        public int DailyWithdrawalCountLimit { get; set; }
    }
}
