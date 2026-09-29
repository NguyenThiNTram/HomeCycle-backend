using HomeCycle.Application.DTOs.Responses.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IFinanceRealtimePublisher
    {
        Task PublishToUserAsync(
            Guid userId,
            FinanceUpdatedResponse response,
            CancellationToken ct = default);

        Task PublishToFinanceGroupAsync(
            FinanceUpdatedResponse response,
            CancellationToken ct = default);
    }
}
