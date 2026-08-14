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
        private readonly ILogger<WalletService> _logger;

        public WalletService(IWalletRepository walletRepo, IWalletLedgerRepository ledgerRepo, ILogger<WalletService> logger)
        {
            _walletRepo = walletRepo;
            _ledgerRepo = ledgerRepo;
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

    }
}
