using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WithdrawalQuotaResponseDto
    {
        public decimal MinimumWithdrawalAmount { get; set; }
        public decimal MaximumWithdrawalAmount { get; set; }
        public decimal DailyWithdrawalLimit { get; set; }

        public decimal CompletedTodayAmount { get; set; }
        public decimal ActiveReservedAmount { get; set; }
        public decimal UsedDailyLimitAmount { get; set; }
        public decimal RemainingDailyLimitAmount { get; set; }

        public DateTimeOffset ResetAt { get; set; }
    }
}
