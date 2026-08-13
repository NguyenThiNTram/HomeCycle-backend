using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public class RejectWithdrawalRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
