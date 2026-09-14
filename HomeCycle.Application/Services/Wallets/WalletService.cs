using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Wallets
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletLedgerRepository _ledgerRepo;
        private readonly IWalletTransactionRepository _walletTxRepo;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IWalletRepository walletRepo, IWalletLedgerRepository ledgerRepo, IWalletTransactionRepository walletTxRepo, ILogger<WalletService> logger)
        {
            _walletRepo = walletRepo;
            _ledgerRepo = ledgerRepo;
            _walletTxRepo = walletTxRepo;
            _logger = logger;
        }

        public async Task<Result<WalletInfoDto>> GetMyWalletAsync(Guid userId, WalletTypeEnum walletType, CancellationToken ct = default)
        {
            var wallet = await _walletRepo.GetByUserIdAndTypeAsync(userId, walletType, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Không tìm thấy ví {WalletType} của user {UserId}", walletType, userId);
                return Result<WalletInfoDto>.Fail(new Error("Wallet.NotFound", "Không tìm thấy ví của người dùng."));
            }

            var dto = new WalletInfoDto
            {
                WalletId = wallet.WalletId,
                WalletType = (WalletTypeEnum)wallet.WalletType,
                AvailableBalance = wallet.AvailableBalance,
                HoldBalance = wallet.HoldBalance,
                Purpose = wallet.Purpose.HasValue ? (SystemWalletPurpose)wallet.Purpose.Value : null
            };
            return Result<WalletInfoDto>.Success(dto);
        }

        public async Task<Result<PagedResult<WalletLedgerResponseDto>>> GetWalletStatementAsync(Guid userId, WalletTypeEnum walletType, WalletLedgerSearchRequest request, CancellationToken ct = default)
        {
            // Xác thực ví thuộc về User trước khi cho xem sao kê (chặn IDOR)
            var wallet = await _walletRepo.GetByUserIdAndTypeAsync(userId, walletType, ct);
            if (wallet == null)
                return Result<PagedResult<WalletLedgerResponseDto>>.Fail(new Error("Wallet.NotFound", "Không tìm thấy ví."));

            var result = await _ledgerRepo.GetPagedByWalletIdAsync(wallet.WalletId, request, ct);
            return Result<PagedResult<WalletLedgerResponseDto>>.Success(result);
        }

        public async Task<Result<SystemWalletSummaryDto>> GetSystemWalletSummaryAsync(CancellationToken ct = default)
        {
            var systemWallets = await _walletRepo.GetAllSystemWalletsAsync(ct);

            var walletDtos = systemWallets.Select(w => new WalletInfoDto
            {
                WalletId = w.WalletId,
                WalletType = (WalletTypeEnum)w.WalletType,
                AvailableBalance = w.AvailableBalance,
                HoldBalance = w.HoldBalance,
                Purpose = w.Purpose.HasValue ? (SystemWalletPurpose)w.Purpose.Value : null
            }).ToList();

            var totalAvailable = walletDtos.Sum(w => w.AvailableBalance);
            var totalHold = walletDtos.Sum(w => w.HoldBalance);

            var summary = new SystemWalletSummaryDto
            {
                Wallets = walletDtos,
                TotalAvailableBalance = totalAvailable,
                TotalHoldBalance = totalHold,
                TotalHeldBalance = totalAvailable + totalHold
            };

            return Result<SystemWalletSummaryDto>.Success(summary);
        }


        public async Task<Result<WalletFinanceFundsDto>> GetFinanceFundsAsync(CancellationToken ct = default)
        {
            var userWallets = await _walletRepo.GetAllUserWalletsAsync(ct);
            var systemWallets = await _walletRepo.GetAllSystemWalletsAsync(ct);

            var systemWalletDtos = systemWallets.Select(w => new WalletInfoDto
            {
                WalletId = w.WalletId,
                WalletType = (WalletTypeEnum)w.WalletType,
                AvailableBalance = w.AvailableBalance,
                HoldBalance = w.HoldBalance,
                Purpose = w.Purpose.HasValue
                    ? (SystemWalletPurpose)w.Purpose.Value
                    : null
            }).ToList();

            var personalWallets = userWallets.Where(x => x.WalletType == (int)WalletTypeEnum.Personal);
            var businessWallets = userWallets.Where(x => x.WalletType == (int)WalletTypeEnum.Business);

            var totalPersonalAvailable = personalWallets.Sum(x => x.AvailableBalance);
            var totalPersonalHold = personalWallets.Sum(x => x.HoldBalance);
            var totalBusinessAvailable = businessWallets.Sum(x => x.AvailableBalance);
            var totalBusinessHold = businessWallets.Sum(x => x.HoldBalance);

            var totalUserAvailable = totalPersonalAvailable + totalBusinessAvailable;
            var totalUserHold = totalPersonalHold + totalBusinessHold;
            var totalSystemAvailable = systemWallets.Sum(x => x.AvailableBalance);
            var totalSystemHold = systemWallets.Sum(x => x.HoldBalance);

            return Result<WalletFinanceFundsDto>.Success(new WalletFinanceFundsDto
            {
                TotalUserAvailable = totalUserAvailable,
                TotalUserHold = totalUserHold,
                TotalPersonalAvailable = totalPersonalAvailable,
                TotalPersonalHold = totalPersonalHold,
                TotalBusinessAvailable = totalBusinessAvailable,
                TotalBusinessHold = totalBusinessHold,
                TotalSystemAvailable = totalSystemAvailable,
                TotalSystemHold = totalSystemHold,
                TotalRecordedBalance =
                    totalUserAvailable +
                    totalUserHold +
                    totalSystemAvailable +
                    totalSystemHold,
                SystemWallets = systemWalletDtos
            });
        }

        public async Task<Result<IReadOnlyList<WalletActiveHoldDto>>> GetActiveHoldsAsync(CancellationToken ct = default)
        {
            var result = await _ledgerRepo.GetActiveHoldsAsync(ct);
            return Result<IReadOnlyList<WalletActiveHoldDto>>.Success(result);
        }

        public async Task<Result<PagedResult<WalletTransactionListItemDto>>> GetFinanceTransactionsAsync(
            WalletTransactionSearchRequest request,
            CancellationToken ct = default)
        {
            var result = await _walletTxRepo.GetPagedAsync(request, ct);

            return Result<PagedResult<WalletTransactionListItemDto>>.Success(result);
        }

        public async Task<Result<WalletTransactionDetailDto>> GetFinanceTransactionDetailAsync(
            Guid walletTransactionId,
            CancellationToken ct = default)
        {
            var transaction = await _walletTxRepo.GetDetailAsync(walletTransactionId, ct);

            if (transaction == null)
            {
                return Result<WalletTransactionDetailDto>.Fail(
                    new Error(
                        "WalletTransaction.NotFound",
                        "Không tìm thấy wallet transaction."));
            }

            return Result<WalletTransactionDetailDto>.Success(transaction);
        }
    }
}
