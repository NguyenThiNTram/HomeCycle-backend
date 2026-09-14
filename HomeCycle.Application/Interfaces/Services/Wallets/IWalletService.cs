using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Wallets
{
    public interface IWalletService
    {
        Task<Result<WalletInfoDto>> GetMyWalletAsync(Guid userId, WalletTypeEnum walletType, CancellationToken ct = default);
        Task<Result<PagedResult<WalletLedgerResponseDto>>> GetWalletStatementAsync(Guid userId, WalletTypeEnum walletType, WalletLedgerSearchRequest request, CancellationToken ct = default);

        Task<Result<SystemWalletSummaryDto>> GetSystemWalletSummaryAsync(CancellationToken ct = default);
        Task<Result<WalletFinanceFundsDto>> GetFinanceFundsAsync(CancellationToken ct = default);
        Task<Result<IReadOnlyList<WalletActiveHoldDto>>> GetActiveHoldsAsync(CancellationToken ct = default);
        Task<Result<PagedResult<WalletTransactionListItemDto>>> GetFinanceTransactionsAsync(
            WalletTransactionSearchRequest request,
            CancellationToken ct = default);
        Task<Result<WalletTransactionDetailDto>> GetFinanceTransactionDetailAsync(
            Guid walletTransactionId,
            CancellationToken ct = default);
    }

}
