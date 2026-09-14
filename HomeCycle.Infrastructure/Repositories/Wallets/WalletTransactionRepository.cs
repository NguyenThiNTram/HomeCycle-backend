using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Wallets
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly HomeCycleDbContext _db;
        public WalletTransactionRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(wallet_transaction transaction, CancellationToken ct = default)
        {
            await _db.Wallet_Transactions.AddAsync(transaction.ToInfrastructure(), ct);
        }

        public async Task<IReadOnlyList<wallet_transaction>> GetByReferenceAsync(
            ReferenceType referenceType,
            Guid referenceId,
            CancellationToken ct = default)
        {
            var entities = await _db.Wallet_Transactions
                .AsNoTracking()
                .Where(x => x.ReferenceType == (int)referenceType && x.ReferenceId == referenceId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.WalletTransactionId)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<PagedResult<WalletTransactionListItemDto>> GetPagedAsync(
            WalletTransactionSearchRequest request,
            CancellationToken ct = default)
        {
            var query = _db.Wallet_Transactions
                .AsNoTracking()
                .AsQueryable();

            if (request.TransactionType.HasValue)
            {
                query = query.Where(x =>
                    x.TransactionType == (int)request.TransactionType.Value);
            }

            if (request.ReferenceType.HasValue)
            {
                query = query.Where(x =>
                    x.ReferenceType == (int)request.ReferenceType.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.WalletTransactionStatus == (int)request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt >= request.FromDate.Value.ToUniversalTime());
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt <= request.ToDate.Value.ToUniversalTime());
            }

            if (request.ReferenceId.HasValue)
                query = query.Where(x => x.ReferenceId == request.ReferenceId.Value);

            if (request.WalletId.HasValue)
            {
                var walletId = request.WalletId.Value;
                query = query.Where(x => x.FromWalletId == walletId || x.ToWalletId == walletId);
            }

            if (request.UserId.HasValue)
            {
                var userId = request.UserId.Value;
                query = query.Where(x =>
                    (x.FromWallet != null && x.FromWallet.UserId == userId) ||
                    (x.ToWallet != null && x.ToWallet.UserId == userId) ||
                    (x.Payment != null && x.Payment.PayerId == userId));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.WalletTransactionId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new WalletTransactionListItemDto
                {
                    PaymentId = x.PaymentId,
                    PaymentMethod = x.Payment != null && x.Payment.PaymentMethod.HasValue
                        ? (PaymentMethod?)x.Payment.PaymentMethod.Value
                        : null,
                    WalletTransactionId = x.WalletTransactionId,
                    From = x.FromWallet == null ? null : new WalletFinancePartyDto
                    {
                        WalletId = x.FromWallet.WalletId,
                        UserId = x.FromWallet.UserId,
                        Username = x.FromWallet.UserId.HasValue ? x.FromWallet.User.Username : null,
                        Role = x.FromWallet.UserId.HasValue ? (UserRole?)x.FromWallet.User.Role : null,
                        WalletType = (WalletTypeEnum)x.FromWallet.WalletType,
                        SystemPurpose = x.FromWallet.Purpose.HasValue
                            ? (SystemWalletPurpose?)x.FromWallet.Purpose.Value
                            : null
                    },
                    To = x.ToWallet == null ? null : new WalletFinancePartyDto
                    {
                        WalletId = x.ToWallet.WalletId,
                        UserId = x.ToWallet.UserId,
                        Username = x.ToWallet.UserId.HasValue ? x.ToWallet.User.Username : null,
                        Role = x.ToWallet.UserId.HasValue ? (UserRole?)x.ToWallet.User.Role : null,
                        WalletType = (WalletTypeEnum)x.ToWallet.WalletType,
                        SystemPurpose = x.ToWallet.Purpose.HasValue
                            ? (SystemWalletPurpose?)x.ToWallet.Purpose.Value
                            : null
                    },
                    ReferenceCode = x.ReferenceType == (int)ReferenceType.Order && x.ReferenceId.HasValue
                        ? _db.Orders.Where(o => o.OrderId == x.ReferenceId.Value)
                            .Select(o => o.OrderCode)
                            .FirstOrDefault()
                        : null,
                    TransactionType = x.TransactionType.HasValue
                        ? (TransactionType)x.TransactionType.Value
                        : null,
                    ReferenceType = x.ReferenceType.HasValue
                        ? (ReferenceType)x.ReferenceType.Value
                        : null,
                    ReferenceId = x.ReferenceId,
                    Amount = x.Amount,
                    Status = x.WalletTransactionStatus.HasValue
                        ? (WalletTransactionStatus)x.WalletTransactionStatus.Value
                        : null,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(ct);

            return new PagedResult<WalletTransactionListItemDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<WalletTransactionDetailDto?> GetDetailAsync(
            Guid walletTransactionId,
            CancellationToken ct = default)
        {
            return await _db.Wallet_Transactions
                .AsNoTracking()
                .Where(x => x.WalletTransactionId == walletTransactionId)
                .Select(x => new WalletTransactionDetailDto
                {
                    WalletTransactionId = x.WalletTransactionId,

                    FromWalletId = x.FromWalletId,
                    ToWalletId = x.ToWalletId,

                    From = x.FromWallet == null
                        ? null
                        : new WalletFinancePartyDto
                        {
                            WalletId = x.FromWallet.WalletId,
                            UserId = x.FromWallet.UserId,
                            Username = x.FromWallet.UserId.HasValue
                                ? x.FromWallet.User.Username
                                : null,
                            Role = x.FromWallet.UserId.HasValue
                                ? (UserRole?)x.FromWallet.User.Role
                                : null,
                            WalletType = (WalletTypeEnum)x.FromWallet.WalletType,
                            SystemPurpose = x.FromWallet.Purpose.HasValue
                                ? (SystemWalletPurpose?)x.FromWallet.Purpose.Value
                                : null
                        },

                    To = x.ToWallet == null
                        ? null
                        : new WalletFinancePartyDto
                        {
                            WalletId = x.ToWallet.WalletId,
                            UserId = x.ToWallet.UserId,
                            Username = x.ToWallet.UserId.HasValue
                                ? x.ToWallet.User.Username
                                : null,
                            Role = x.ToWallet.UserId.HasValue
                                ? (UserRole?)x.ToWallet.User.Role
                                : null,
                            WalletType = (WalletTypeEnum)x.ToWallet.WalletType,
                            SystemPurpose = x.ToWallet.Purpose.HasValue
                                ? (SystemWalletPurpose?)x.ToWallet.Purpose.Value
                                : null
                        },

                    PaymentId = x.PaymentId,
                    PaymentMethod = x.Payment != null && x.Payment.PaymentMethod.HasValue
                        ? (PaymentMethod?)x.Payment.PaymentMethod.Value
                        : null,

                    ReferenceId = x.ReferenceId,
                    ReferenceType = x.ReferenceType.HasValue
                        ? (ReferenceType)x.ReferenceType.Value
                        : null,

                    ReferenceCode = x.ReferenceType == (int)ReferenceType.Order
                        && x.ReferenceId.HasValue
                        ? _db.Orders
                            .Where(o => o.OrderId == x.ReferenceId.Value)
                            .Select(o => o.OrderCode)
                            .FirstOrDefault()
                        : null,

                    TransactionType = x.TransactionType.HasValue
                        ? (TransactionType)x.TransactionType.Value
                        : null,

                    Amount = x.Amount,

                    Status = x.WalletTransactionStatus.HasValue
                        ? (WalletTransactionStatus)x.WalletTransactionStatus.Value
                        : null,

                    CreatedAt = x.CreatedAt,

                    Ledgers = x.Wallet_Ledgers
                        .OrderBy(l => l.CreatedAt)
                        .ThenBy(l => l.LedgerId)
                        .Select(l => new WalletLedgerResponseDto
                        {
                            LedgerId = l.LedgerId,
                            CreatedAt = l.CreatedAt,
                            Direction = (LedgerDirection)l.Direction,
                            BalanceType = (BalanceType)l.BalanceType,
                            Amount = l.Amount,
                            BalanceBefore = l.BalanceBefore,
                            BalanceAfter = l.BalanceAfter,
                            Description = l.Description ?? string.Empty,
                            ReferenceType = l.ReferenceType.HasValue
                                ? (ReferenceType)l.ReferenceType.Value
                                : null,
                            ReferenceId = l.ReferenceId,
                            TransactionType = l.WalletTransaction.TransactionType.HasValue
                                ? (TransactionType)l.WalletTransaction.TransactionType.Value
                                : null
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync(ct);
        }

    }
}
