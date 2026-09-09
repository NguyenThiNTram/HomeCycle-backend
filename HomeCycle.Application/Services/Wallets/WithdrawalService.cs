using FluentValidation;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Wallets
{
    public class WithdrawalService : IWithdrawalService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBankAccountRepository _bankAccountRepo;
        private readonly IPayoutGatewayService _payoutGateway;
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _walletTxRepo;
        private readonly IWalletLedgerRepository _ledgerRepo;
        private readonly IWithdrawalRepository _withdrawalRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WithdrawalService> _logger;
        private readonly IValidator<CreateWithdrawalRequest> _createValidator;
        private readonly IValidator<RejectWithdrawalRequest> _rejectValidator;

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            IBankAccountRepository bankAccountRepo,
            IPayoutGatewayService payoutGateway,
            IWalletRepository walletRepo,
            IWalletTransactionRepository walletTxRepo,
            IWalletLedgerRepository ledgerRepo,
            IWithdrawalRepository withdrawalRepo,
            INotificationService notificationService,
            ILogger<WithdrawalService> logger,
            IValidator<CreateWithdrawalRequest> createValidator,
            IValidator<RejectWithdrawalRequest> rejectValidator)
        {
            _unitOfWork = unitOfWork;
            _bankAccountRepo = bankAccountRepo;
            _payoutGateway = payoutGateway;
            _walletRepo = walletRepo;
            _walletTxRepo = walletTxRepo;
            _ledgerRepo = ledgerRepo;
            _withdrawalRepo = withdrawalRepo;
            _notificationService = notificationService;
            _createValidator = createValidator;
            _rejectValidator = rejectValidator;
            _logger = logger;
        }


        public async Task<Result<Guid>> CreateWithdrawalRequestAsync(
            Guid userId, CreateWithdrawalRequest request, CancellationToken ct = default)
        {
            var validationResult = await _createValidator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<Guid>.Fail(new Error("Withdrawal.InvalidRequest", errors));
            }

            var amount = request.Amount;

            var bankAccount = await _bankAccountRepo.GetByUserIdAsync(userId, ct);
            if (bankAccount == null || bankAccount.VerifyStatus != VerifyStatus.Verified)
                return Result<Guid>.Fail(new Error(
                    "Withdrawal.BankAccountNotVerified",
                    "Vui lòng thêm và xác thực tài khoản ngân hàng trước khi rút tiền."));

            var wallet = await _walletRepo.GetByUserIdAndTypeAsync(userId, WalletTypeEnum.Personal, ct);
            if (wallet == null || wallet.AvailableBalance < amount)
                return Result<Guid>.Fail(new Error("Wallet.InsufficientBalance", "Số dư khả dụng không đủ."));

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                var withdrawalId = Guid.NewGuid();

                var withdrawalEntity = new withdrawal
                {
                    WithdrawalId = withdrawalId,
                    WalletId = wallet.WalletId,
                    UserBankId = bankAccount.UserBankId,
                    Amount = amount,
                    WithdrawalStatus = (int)WithdrawalStatus.Pending,
                    RequestedAt = now
                };
                await _withdrawalRepo.AddAsync(withdrawalEntity, ct);

                var walletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = wallet.WalletId,
                    ReferenceId = withdrawalId,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    TransactionType = (int)TransactionType.Withdrawal_Lock,
                    Amount = -amount,
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = now
                };
                await _walletTxRepo.AddAsync(walletTx, ct);

                var ledgerOut = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = walletTx.WalletTransactionId,
                    WalletId = wallet.WalletId,
                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Available,
                    Amount = amount,
                    BalanceBefore = wallet.AvailableBalance,
                    BalanceAfter = wallet.AvailableBalance - amount,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    ReferenceId = withdrawalId,
                    Description = $"Khoa tien cho yeu cau rut {withdrawalId}",
                    CreatedAt = now
                };
                await _ledgerRepo.AddAsync(ledgerOut, ct);

                var ledgerIn = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = walletTx.WalletTransactionId,
                    WalletId = wallet.WalletId,
                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = amount,
                    BalanceBefore = wallet.HoldBalance,
                    BalanceAfter = wallet.HoldBalance + amount,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    ReferenceId = withdrawalId,
                    Description = $"Khoa tien cho yeu cau rut {withdrawalId}",
                    CreatedAt = now
                };
                await _ledgerRepo.AddAsync(ledgerIn, ct);

                wallet.AvailableBalance -= amount;
                wallet.HoldBalance += amount;
                wallet.UpdatedAt = now;
                await _walletRepo.UpdateAsync(wallet, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<Guid>.Success(withdrawalId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Lỗi tạo yêu cầu rút tiền cho user {UserId}", userId);
                return Result<Guid>.Fail(new Error("Withdrawal.CreateFailed", "Không thể tạo yêu cầu rút tiền."));
            }
        }

        public async Task<Result<bool>> ApproveWithdrawalAsync(
            Guid moderatorId, Guid withdrawalId, CancellationToken ct = default)
        {
            var withdrawalEntity = await _withdrawalRepo.GetByIdAsync(withdrawalId, ct);
            if (withdrawalEntity == null)
                return Result<bool>.Fail(new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));

            if (withdrawalEntity.WithdrawalStatus != (int)WithdrawalStatus.Pending)
                return Result<bool>.Fail(new Error("Withdrawal.InvalidStatus", "Yêu cầu không ở trạng thái chờ duyệt."));

            var bankAccount = await _bankAccountRepo.GetByIdAsync(
                withdrawalEntity.UserBankId,
                ct);
            if (bankAccount == null || bankAccount.VerifyStatus != VerifyStatus.Verified)
                return Result<bool>.Fail(new Error("Withdrawal.BankAccountInvalid", "Tài khoản ngân hàng không hợp lệ/chưa xác thực."));

            var payoutResult = await _payoutGateway.CreatePayoutAsync(new GatewayPayoutRequest
            {
                ReferenceId = withdrawalId.ToString(),
                Amount = (int)withdrawalEntity.Amount!.Value,
                Description = $"Rut tien {withdrawalId.ToString()[..8]}",
                ToBin = bankAccount.BankCode!,
                ToAccountNumber = bankAccount.AccountNumber!
            }, ct);

            if (!payoutResult.IsSuccess)
            {
                _logger.LogError("Gọi payOS Payout thất bại cho Withdrawal {WithdrawalId}: {Error}",
                    withdrawalId, payoutResult.Error.Message);
                return Result<bool>.Fail(payoutResult.Error);
            }

            withdrawalEntity.WithdrawalStatus = (int)WithdrawalStatus.Processing;
            withdrawalEntity.ProcessedAt = DateTime.UtcNow;
            withdrawalEntity.ProcessedBy = moderatorId;
            await _withdrawalRepo.UpdateAsync(withdrawalEntity, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await Task.Delay(2000, ct);
            await SyncWithdrawalStatusAsync(withdrawalId, ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> RejectWithdrawalAsync(
            Guid moderatorId, Guid withdrawalId, RejectWithdrawalRequest request, CancellationToken ct = default)
        {
            var validationResult = await _rejectValidator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
                return Result<bool>.Fail(new Error("Withdrawal.InvalidRequest", errors));
            }

            var withdrawalEntity = await _withdrawalRepo.GetByIdAsync(withdrawalId, ct);
            if (withdrawalEntity == null)
                return Result<bool>.Fail(new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));

            if (withdrawalEntity.WithdrawalStatus != (int)WithdrawalStatus.Pending)
                return Result<bool>.Fail(new Error("Withdrawal.InvalidStatus", "Yêu cầu không ở trạng thái chờ duyệt."));

            withdrawalEntity.RejectReason = request.Reason;
            withdrawalEntity.ProcessedBy = moderatorId;
            await RevertHoldToAvailableAsync(withdrawalEntity, WithdrawalStatus.Rejected, request.Reason, ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> SyncWithdrawalStatusAsync(Guid withdrawalId, CancellationToken ct = default)
        {
            var withdrawalEntity = await _withdrawalRepo.GetByIdAsync(withdrawalId, ct);
            if (withdrawalEntity == null)
                return Result<bool>.Fail(new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));

            if (withdrawalEntity.WithdrawalStatus != (int)WithdrawalStatus.Processing)
                return Result<bool>.Success(true);

            var statusResult = await _payoutGateway.GetPayoutStatusAsync(withdrawalId.ToString(), ct);
            if (!statusResult.IsSuccess)
                return Result<bool>.Fail(statusResult.Error);

            var data = statusResult.Data!;

            if (data.ApprovalState is "REJECTED" or "CANCELLED")
            {
                // Hệ thống tự phát hiện thất bại — KHÔNG đổi ProcessedBy (giữ nguyên moderator đã Approve).
                await RevertHoldToAvailableAsync(
                    withdrawalEntity, WithdrawalStatus.Failed,
                    reason: $"payOS từ chối lệnh chi (ApprovalState: {data.ApprovalState})", ct);
                return Result<bool>.Success(true);
            }

            switch (data.TransactionState)
            {
                case "SUCCEEDED":
                    await FinalizeSuccessAsync(withdrawalEntity, ct);
                    break;
                case "FAILED":
                    await RevertHoldToAvailableAsync(
                        withdrawalEntity, WithdrawalStatus.Failed,
                        reason: data.FailureReason ?? "Giao dịch chuyển tiền thất bại từ payOS.", ct);
                    break;
                default:
                    break; // vẫn Processing, chưa có gì để làm
            }

            return Result<bool>.Success(true);
        }


        // ================== HELPERS DÙNG CHUNG ==================

        private async Task FinalizeSuccessAsync(
            withdrawal withdrawalEntity,
            CancellationToken ct)
        {
            notification? completedNotification = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var lockedWithdrawal =
                    await _withdrawalRepo.GetByIdForUpdateAsync(
                        withdrawalEntity.WithdrawalId,
                        ct);

                if (lockedWithdrawal == null)
                    throw new InvalidOperationException(
                        "Không tìm thấy yêu cầu rút tiền.");

                if (lockedWithdrawal.WithdrawalStatus ==
                    (int)WithdrawalStatus.Completed)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return;
                }

                if (lockedWithdrawal.WithdrawalStatus !=
                    (int)WithdrawalStatus.Processing)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return;
                }

                var wallet = await _walletRepo.GetByIdAsync(
                    lockedWithdrawal.WalletId,
                    ct);

                if (wallet?.UserId is not Guid recipientId)
                    throw new InvalidOperationException(
                        "Không tìm thấy người sở hữu ví.");

                var amount = lockedWithdrawal.Amount!.Value;
                var now = DateTime.UtcNow;

                var walletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = wallet.WalletId,
                    ReferenceId = lockedWithdrawal.WithdrawalId,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    TransactionType = (int)TransactionType.Withdrawal_Success,
                    Amount = -amount,
                    WalletTransactionStatus =
                        (int)WalletTransactionStatus.Completed,
                    CreatedAt = now
                };

                var ledger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = walletTx.WalletTransactionId,
                    WalletId = wallet.WalletId,
                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = amount,
                    BalanceBefore = wallet.HoldBalance,
                    BalanceAfter = wallet.HoldBalance - amount,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    ReferenceId = lockedWithdrawal.WithdrawalId,
                    Description =
                        $"Rut tien thanh cong {lockedWithdrawal.WithdrawalId}",
                    CreatedAt = now
                };

                wallet.HoldBalance -= amount;
                wallet.UpdatedAt = now;

                lockedWithdrawal.WithdrawalStatus =
                    (int)WithdrawalStatus.Completed;

                await _walletTxRepo.AddAsync(walletTx, ct);
                await _ledgerRepo.AddAsync(ledger, ct);
                await _walletRepo.UpdateAsync(wallet, ct);
                await _withdrawalRepo.UpdateAsync(lockedWithdrawal, ct);

                completedNotification =
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            recipientId,
                            "Rút tiền thành công",
                            "Khoản tiền rút đã được chuyển thành công tới tài khoản ngân hàng của bạn.",
                            NotificationTargetType.Withdrawal,
                            lockedWithdrawal.WithdrawalId),
                        ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);

                _logger.LogError(
                    ex,
                    "Lỗi hạch toán Withdrawal_Success cho {WithdrawalId}",
                    withdrawalEntity.WithdrawalId);

                throw;
            }

            await _notificationService.PublishCreatedSafelyAsync(
                completedNotification);
        }

        private async Task RevertHoldToAvailableAsync(
            withdrawal withdrawalEntity,
            WithdrawalStatus finalStatus,
            string reason,
            CancellationToken ct)
        {
            notification? revertedNotification = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var lockedWithdrawal =
                    await _withdrawalRepo.GetByIdForUpdateAsync(
                        withdrawalEntity.WithdrawalId,
                        ct);

                if (lockedWithdrawal == null)
                    throw new InvalidOperationException(
                        "Không tìm thấy yêu cầu rút tiền.");

                if (lockedWithdrawal.WithdrawalStatus == (int)finalStatus)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return;
                }

                var expectedStatus = finalStatus == WithdrawalStatus.Rejected
                    ? WithdrawalStatus.Pending
                    : WithdrawalStatus.Processing;

                if (lockedWithdrawal.WithdrawalStatus !=
                    (int)expectedStatus)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return;
                }

                var wallet = await _walletRepo.GetByIdAsync(
                    lockedWithdrawal.WalletId,
                    ct);

                if (wallet?.UserId is not Guid recipientId)
                    throw new InvalidOperationException(
                        "Không tìm thấy người sở hữu ví.");

                var amount = lockedWithdrawal.Amount!.Value;
                var now = DateTime.UtcNow;

                var walletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = wallet.WalletId,
                    ReferenceId = lockedWithdrawal.WithdrawalId,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    TransactionType =
                        (int)TransactionType.Withdrawal_Revert,
                    Amount = amount,
                    WalletTransactionStatus =
                        (int)WalletTransactionStatus.Completed,
                    CreatedAt = now
                };

                var ledgerOutHold = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = walletTx.WalletTransactionId,
                    WalletId = wallet.WalletId,
                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = amount,
                    BalanceBefore = wallet.HoldBalance,
                    BalanceAfter = wallet.HoldBalance - amount,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    ReferenceId = lockedWithdrawal.WithdrawalId,
                    Description = $"Hoan tien: {reason}",
                    CreatedAt = now
                };

                var ledgerInAvailable = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = walletTx.WalletTransactionId,
                    WalletId = wallet.WalletId,
                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Available,
                    Amount = amount,
                    BalanceBefore = wallet.AvailableBalance,
                    BalanceAfter = wallet.AvailableBalance + amount,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    ReferenceId = lockedWithdrawal.WithdrawalId,
                    Description = $"Hoan tien: {reason}",
                    CreatedAt = now
                };

                wallet.HoldBalance -= amount;
                wallet.AvailableBalance += amount;
                wallet.UpdatedAt = now;

                lockedWithdrawal.WithdrawalStatus = (int)finalStatus;
                lockedWithdrawal.RejectReason = reason;

                if (finalStatus == WithdrawalStatus.Rejected)
                    lockedWithdrawal.ProcessedBy =
                        withdrawalEntity.ProcessedBy;

                await _walletTxRepo.AddAsync(walletTx, ct);
                await _ledgerRepo.AddAsync(ledgerOutHold, ct);
                await _ledgerRepo.AddAsync(ledgerInAvailable, ct);
                await _walletRepo.UpdateAsync(wallet, ct);
                await _withdrawalRepo.UpdateAsync(lockedWithdrawal, ct);

                var notificationTitle =
                    finalStatus == WithdrawalStatus.Rejected
                        ? "Yêu cầu rút tiền bị từ chối"
                        : "Rút tiền không thành công";

                var notificationMessage =
                    finalStatus == WithdrawalStatus.Rejected
                        ? $"Yêu cầu rút tiền đã bị từ chối. Lý do: {reason}"
                        : "Giao dịch rút tiền không thành công. Toàn bộ số tiền tạm giữ đã được trả lại số dư khả dụng.";

                revertedNotification =
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            recipientId,
                            notificationTitle,
                            notificationMessage,
                            NotificationTargetType.Withdrawal,
                            lockedWithdrawal.WithdrawalId),
                        ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);

                _logger.LogError(
                    ex,
                    "Lỗi hoàn tiền Withdrawal_Revert cho {WithdrawalId}",
                    withdrawalEntity.WithdrawalId);

                throw;
            }

            await _notificationService.PublishCreatedSafelyAsync(
                revertedNotification);
        }
    }
}
