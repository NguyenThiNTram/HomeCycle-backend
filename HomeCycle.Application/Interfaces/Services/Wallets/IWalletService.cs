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

    }

}
