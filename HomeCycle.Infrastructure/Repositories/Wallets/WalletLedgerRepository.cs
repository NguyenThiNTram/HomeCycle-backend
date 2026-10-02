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
        private const decimal AmountEpsilon = 0.01m;
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
            BalanceType balanceType,
            CancellationToken ct = default)
        {
            var amount = await _db.Wallet_Ledgers
                .AsNoTracking()
                .Where(x =>
                    x.WalletId == walletId &&
                    x.BalanceType == (int)balanceType &&
                    x.ReferenceType == (int)ReferenceType.Order &&
                    x.ReferenceId == orderId)
                .Select(x => (decimal?)(
                    x.Direction == (int)LedgerDirection.In
                        ? x.Amount
                        : -x.Amount))
                .SumAsync(ct);

            return amount ?? 0;
        }

        public async Task<IReadOnlyList<WalletActiveHoldDto>> GetActiveHoldsAsync(CancellationToken ct = default)
        {
            return await _db.Wallet_Ledgers
                .AsNoTracking()
                .Where(x =>
                    x.BalanceType == (int)BalanceType.Hold &&
                    x.ReferenceType != null &&
                    x.ReferenceId != null)
                .GroupBy(x => new
                {
                    x.WalletId,
                    x.Wallet.WalletType,
                    x.Wallet.Purpose,
                    x.Wallet.UserId,
                    Username = x.Wallet.UserId.HasValue ? x.Wallet.User.Username : null,
                    UserRole = x.Wallet.UserId.HasValue ? (int?)x.Wallet.User.Role : null,
                    x.ReferenceType,
                    x.ReferenceId
                })
                .Select(g => new WalletActiveHoldDto
                {
                    WalletId = g.Key.WalletId,
                    Owner = new WalletFinancePartyDto
                    {
                        WalletId = g.Key.WalletId,
                        UserId = g.Key.UserId,
                        Username = g.Key.Username,
                        Role = g.Key.UserRole.HasValue ? (UserRole?)g.Key.UserRole.Value : null,
                        WalletType = (WalletTypeEnum)g.Key.WalletType,
                        SystemPurpose = g.Key.Purpose.HasValue ? (SystemWalletPurpose?)g.Key.Purpose.Value : null
                    },
                    ReferenceType = (ReferenceType?)g.Key.ReferenceType,
                    ReferenceId = g.Key.ReferenceId,
                    ReferenceCode = g.Key.ReferenceType == (int)ReferenceType.Order && g.Key.ReferenceId.HasValue
                        ? _db.Orders.Where(o => o.OrderId == g.Key.ReferenceId.Value).Select(o => o.OrderCode).FirstOrDefault()
                        : null,
                    HoldAmount = g.Sum(x => x.Direction == (int)LedgerDirection.In ? x.Amount : -x.Amount)
                })
                .Where(x => x.HoldAmount > 0)
                .OrderByDescending(x => x.HoldAmount)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<OrderEscrowPositionDto>> GetActiveOrderEscrowsAsync(
            OrderEscrowSearchRequest request,
            CancellationToken ct = default)
        {
            var escrowWalletId = await _db.Wallets
                .AsNoTracking()
                .Where(x =>
                    x.UserId == null &&
                    x.WalletType == (int)WalletTypeEnum.System &&
                    x.Purpose == (int)SystemWalletPurpose.Order_Escrow)
                .Select(x => (Guid?)x.WalletId)
                .SingleOrDefaultAsync(ct);

            if (!escrowWalletId.HasValue)
            {
                return new PagedResult<OrderEscrowPositionDto>
                {
                    Items = new List<OrderEscrowPositionDto>(),
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = 0
                };
            }

            var activeDisputeStatuses = new[]
            {
                (int)DisputeStatus.AwaitingResponse,
                (int)DisputeStatus.Pending,
                (int)DisputeStatus.UnderReview,
                (int)DisputeStatus.AwaitingReturn
            };

            var query = _db.Orders
                .AsNoTracking()
                .Select(x => new
                {
                    x.OrderId,
                    x.OrderCode,
                    x.ProductName,

                    BuyerId = x.Agreement.BuyerId,
                    BuyerUsername = x.Agreement.Buyer.Username,
                    SellerId = x.Agreement.SellerId,
                    SellerUsername = x.Agreement.Seller.Username,

                    x.OrderStatus,
                    x.PaymentStatus,
                    x.DisputeWindowEndsAt,
                    x.UpdatedAt,

                    HasActiveDispute = x.Disputes.Any(d =>
                        d.DisputeStatus.HasValue &&
                        activeDisputeStatuses.Contains(
                            d.DisputeStatus.Value)),

                    EscrowAmount = _db.Wallet_Ledgers
                        .Where(l =>
                            l.WalletId == escrowWalletId.Value &&
                            l.BalanceType == (int)BalanceType.Available &&
                            l.ReferenceType == (int)ReferenceType.Order &&
                            l.ReferenceId == x.OrderId)
                        .Select(l => (decimal?)(
                            l.Direction == (int)LedgerDirection.In
                                ? l.Amount
                                : -l.Amount))
                        .Sum() ?? 0m
                })
                .Where(x => x.EscrowAmount > AmountEpsilon);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.OrderCode, $"%{keyword}%") ||
                    EF.Functions.ILike(x.BuyerUsername, $"%{keyword}%") ||
                    EF.Functions.ILike(x.SellerUsername, $"%{keyword}%"));
            }

            if (request.OrderStatus.HasValue)
            {
                query = query.Where(x =>
                    x.OrderStatus ==
                    (int)request.OrderStatus.Value);
            }

            if (request.PaymentStatus.HasValue)
            {
                query = query.Where(x =>
                    x.PaymentStatus ==
                    (int)request.PaymentStatus.Value);
            }

            if (request.HasActiveDispute.HasValue)
            {
                query = query.Where(x =>
                    x.HasActiveDispute ==
                    request.HasActiveDispute.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .ThenBy(x => x.OrderCode)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new OrderEscrowPositionDto
                {
                    OrderId = x.OrderId,
                    OrderCode = x.OrderCode,
                    ProductName = x.ProductName,

                    BuyerId = x.BuyerId,
                    BuyerUsername = x.BuyerUsername,
                    SellerId = x.SellerId,
                    SellerUsername = x.SellerUsername,

                    EscrowAmount = x.EscrowAmount,

                    OrderStatus = x.OrderStatus.HasValue
                        ? (OrderStatus?)x.OrderStatus.Value
                        : null,

                    PaymentStatus = x.PaymentStatus.HasValue
                        ? (PaymentStatus?)x.PaymentStatus.Value
                        : null,

                    DisputeWindowEndsAt =
                        x.DisputeWindowEndsAt,

                    HasActiveDispute =
                        x.HasActiveDispute,

                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return new PagedResult<OrderEscrowPositionDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<SellerPendingSettlementsDto> GetSellerPendingSettlementsAsync(
            Guid sellerId,
            CancellationToken ct = default)
        {
            var escrowWalletId = await _db.Wallets
                .AsNoTracking()
                .Where(x =>
                    x.UserId == null &&
                    x.WalletType == (int)WalletTypeEnum.System &&
                    x.Purpose == (int)SystemWalletPurpose.Order_Escrow)
                .Select(x => (Guid?)x.WalletId)
                .SingleOrDefaultAsync(ct);

            if (!escrowWalletId.HasValue)
                return new SellerPendingSettlementsDto();

            var query = _db.Orders
                .AsNoTracking()
                .Where(x => x.Agreement.SellerId == sellerId)
                .Select(x => new
                {
                    x.OrderId,
                    x.OrderCode,
                    x.ProductName,
                    x.OrderStatus,
                    x.UpdatedAt,
                    EscrowAmount = _db.Wallet_Ledgers
                        .Where(l =>
                            l.WalletId == escrowWalletId.Value &&
                            l.BalanceType == (int)BalanceType.Available &&
                            l.ReferenceType == (int)ReferenceType.Order &&
                            l.ReferenceId == x.OrderId)
                        .Select(l => (decimal?)(l.Direction == (int)LedgerDirection.In ? l.Amount : -l.Amount))
                        .Sum() ?? 0m
                })
                .Where(x => x.EscrowAmount > AmountEpsilon);

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .ThenBy(x => x.OrderCode)
                .Select(x => new SellerPendingSettlementItemDto
                {
                    OrderId = x.OrderId,
                    OrderCode = x.OrderCode,
                    ProductName = x.ProductName,
                    Amount = x.EscrowAmount,
                    OrderStatus = x.OrderStatus.HasValue ? (OrderStatus?)x.OrderStatus.Value : null,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return new SellerPendingSettlementsDto
            {
                TotalPendingAmount = items.Sum(x => x.Amount),
                Items = items
            };
        }
    }
}
