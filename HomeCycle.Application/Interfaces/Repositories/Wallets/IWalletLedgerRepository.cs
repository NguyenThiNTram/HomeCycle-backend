using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWalletLedgerRepository
    {
        Task AddAsync(wallet_ledger ledger, CancellationToken ct = default);
        Task<PagedResult<WalletLedgerResponseDto>> GetPagedByWalletIdAsync(Guid walletId, WalletLedgerSearchRequest request, CancellationToken ct = default);

        Task<decimal> GetNetOrderHeldAmountAsync(
            Guid walletId,
            Guid orderId,
            CancellationToken ct = default);
    }
}
