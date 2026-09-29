using HomeCycle.Application.DTOs.Requests.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Wallets
{
    public interface IFinanceRealtimeService
    {
        Task PublishUpdatedSafelyAsync(FinanceRealtimeChange change);
    }
}
