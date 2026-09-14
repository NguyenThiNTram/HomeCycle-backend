using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWalletTransactionRepository
    {
        Task AddAsync(wallet_transaction transaction, CancellationToken ct = default);
        Task<IReadOnlyList<wallet_transaction>> GetByReferenceAsync(
            ReferenceType referenceType,
            Guid referenceId,
            CancellationToken ct = default);
        Task<PagedResult<WalletTransactionListItemDto>> GetPagedAsync(
            WalletTransactionSearchRequest request,
            CancellationToken ct = default);
        Task<WalletTransactionDetailDto?> GetDetailAsync(
            Guid walletTransactionId,
            CancellationToken ct = default);
    }
}
