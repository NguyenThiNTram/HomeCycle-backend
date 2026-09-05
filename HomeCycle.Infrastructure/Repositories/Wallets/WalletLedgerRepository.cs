using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using MathNet.Numerics.RootFinding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Wallets
{
    public class WalletLedgerRepository : IWalletLedgerRepository
    {
        private readonly HomeCycleDbContext _db;
        public WalletLedgerRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(wallet_ledger ledger, CancellationToken ct = default)
        {

            await _db.Wallet_Ledgers.AddAsync(ledger.ToInfrastructure(), ct);
        }

        public async Task<PagedResult<WalletLedgerResponseDto>> GetPagedByWalletIdAsync(Guid walletId, WalletLedgerSearchRequest request, CancellationToken ct = default)
        {
            var query = _db.Wallet_Ledgers
                .AsNoTracking()
                .Where(x => x.WalletId == walletId);

            if (request.Direction.HasValue)
                query = query.Where(x => x.Direction == (int)request.Direction.Value);

            if (request.BalanceType.HasValue)
                query = query.Where(x => x.BalanceType == (int)request.BalanceType.Value);

            if (request.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= request.FromDate.Value.ToUniversalTime());

            if (request.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= request.ToDate.Value.ToUniversalTime());

            var totalCount = await query.CountAsync(ct);

            // ReferenceType/ReferenceId đã có sẵn ngay trên Wallet_Ledger (không cần join sang Wallet_Transaction để lấy 2 cột này),
            // chỉ thực sự cần join để lấy TransactionType (chỉ tồn tại ở Wallet_Transaction).
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new WalletLedgerResponseDto
                {
                    LedgerId = x.LedgerId,
                    CreatedAt = x.CreatedAt,
                    Direction = (LedgerDirection)x.Direction,
                    BalanceType = (BalanceType)x.BalanceType,
                    Amount = x.Amount,
                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,
                    Description = x.Description ?? string.Empty,
                    ReferenceType = x.ReferenceType.HasValue ? (ReferenceType)x.ReferenceType.Value : null,
                    ReferenceId = x.ReferenceId,
                    TransactionType = x.WalletTransaction != null && x.WalletTransaction.TransactionType.HasValue
                        ? (TransactionType)x.WalletTransaction.TransactionType.Value
                        : null
                })
                .ToListAsync(ct);

            return new PagedResult<WalletLedgerResponseDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<decimal> GetNetOrderHeldAmountAsync(
            Guid walletId,
            Guid orderId,
            CancellationToken ct = default)
        {
            var amount = await _db.Wallet_Ledgers
                .AsNoTracking()
                .Where(x =>
                    x.WalletId == walletId &&
                    x.BalanceType == (int)BalanceType.Hold &&
                    x.ReferenceType == (int)ReferenceType.Order &&
                    x.ReferenceId == orderId)
                .Select(x => (decimal?)(x.Direction == (int)LedgerDirection.In
                    ? x.Amount
                    : -x.Amount))
                .SumAsync(ct);

            return amount ?? 0;
        }
    }
}
