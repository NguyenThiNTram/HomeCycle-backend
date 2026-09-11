using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public class ModeratorWithdrawalSearchRequest : WithdrawalSearchRequest
    {
        public string? Keyword { get; set; }
        public Guid? UserId { get; set; }
    }
}
