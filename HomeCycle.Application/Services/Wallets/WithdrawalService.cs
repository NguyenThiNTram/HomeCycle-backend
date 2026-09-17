using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Banks;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
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
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IBankAccountRepository _bankAccountRepo;
        private readonly IPayoutGatewayService _payoutGateway;
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _walletTxRepo;
        private readonly IWalletLedgerRepository _ledgerRepo;
        private readonly IWithdrawalRepository _withdrawalRepo;
        private readonly INotificationService _notificationService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly IAuditService _auditService;
        private readonly TimeProvider _clock;
        private readonly ILogger<WithdrawalService> _logger;
        private readonly IValidator<CreateWithdrawalRequest> _createValidator;
        private readonly IValidator<RejectWithdrawalRequest> _rejectValidator;
        private readonly IMapper _mapper;

        public WithdrawalService(
            IUnitOfWork unitOfWork,
            IBankAccountRepository bankAccountRepo,
            IPayoutGatewayService payoutGateway,
            IWalletRepository walletRepo,
            IWalletTransactionRepository walletTxRepo,
            IWalletLedgerRepository ledgerRepo,
            IWithdrawalRepository withdrawalRepo,
            INotificationService notificationService,
            IPlatformPolicyProvider platformPolicyProvider,
            IAuditService auditService,
            TimeProvider clock,
            ILogger<WithdrawalService> logger,
            IValidator<CreateWithdrawalRequest> createValidator,
            IValidator<RejectWithdrawalRequest> rejectValidator,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _bankAccountRepo = bankAccountRepo;
            _payoutGateway = payoutGateway;
            _walletRepo = walletRepo;
            _walletTxRepo = walletTxRepo;
            _ledgerRepo = ledgerRepo;
            _withdrawalRepo = withdrawalRepo;
            _notificationService = notificationService;
            _platformPolicyProvider = platformPolicyProvider;
            _auditService = auditService;
            _clock = clock;
            _createValidator = createValidator;
            _rejectValidator = rejectValidator;
            _logger = logger;
            _mapper = mapper;
        }


        public async Task<Result<Guid>> CreateWithdrawalRequestAsync(Guid userId, CreateWithdrawalRequest request, CancellationToken ct = default)
        {
            var validation = await _createValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors.Select(x => x.ErrorMessage));
                return Result<Guid>.Fail(new Error("Withdrawal.InvalidRequest", errors));
            }

            var policy = await _platformPolicyProvider.GetWithdrawalConfigAsync(ct);
            var amount = request.Amount;

            if (amount < policy.MinimumWithdrawalAmount)
                return Result<Guid>.Fail(new Error(
                    "Withdrawal.BelowMinimum",
                    $"Số tiền rút tối thiểu là {policy.MinimumWithdrawalAmount:N0} VNĐ."));

            if (amount > policy.MaximumWithdrawalAmount)
                return Result<Guid>.Fail(new Error(
                    "Withdrawal.AboveMaximum",
                    $"Số tiền rút tối đa mỗi lần là {policy.MaximumWithdrawalAmount:N0} VNĐ."));

            var bankAccount = await _bankAccountRepo.GetByUserIdAsync(userId, ct);
            if (bankAccount == null || bankAccount.VerifyStatus != VerifyStatus.Verified)
                return Result<Guid>.Fail(new Error(
                    "Withdrawal.BankAccountNotVerified",
                    "Vui lòng thêm và xác thực tài khoản ngân hàng trước khi rút tiền."));

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var wallet = await _walletRepo.GetUserWalletForUpdateAsync(userId, ct);
                if (wallet == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<Guid>.Fail(new Error("Wallet.NotFound", "Không tìm thấy ví của người dùng."));
                }

                if (wallet.AvailableBalance < amount)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<Guid>.Fail(new Error("Wallet.InsufficientBalance", "Số dư khả dụng không đủ."));
                }

                var nowUtc = _clock.GetUtcNow();
                var window = GetVietnamDayWindow(nowUtc);
                var usage = await _withdrawalRepo.GetDailyUsageAsync(userId, window.FromUtc, window.ToUtc, ct);
                var usedLimit = usage.CompletedAmount + usage.ReservedAmount;

                if (usedLimit + amount > policy.DailyWithdrawalLimit)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    var remaining = Math.Max(policy.DailyWithdrawalLimit - usedLimit, 0m);

                    return Result<Guid>.Fail(new Error(
                        "Withdrawal.DailyLimitExceeded",
                        $"Hạn mức rút còn lại hôm nay là {remaining:N0} VNĐ."));
                }

                if (usage.UsedCount >= policy.DailyWithdrawalCountLimit)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<Guid>.Fail(new Error(
                        "Withdrawal.DailyCountLimitExceeded",
                        $"Bạn đã sử dụng hết {policy.DailyWithdrawalCountLimit} lượt rút tiền trong ngày."));
                }

                var now = nowUtc.UtcDateTime;
                var withdrawalId = Guid.NewGuid();

                var withdrawal = new withdrawal
                {
                    WithdrawalId = withdrawalId,
                    WalletId = wallet.WalletId,
                    UserBankId = bankAccount.UserBankId,
                    Amount = amount,
                    WithdrawalStatus = (int)WithdrawalStatus.Pending,
                    RequestedAt = now
                };

                var withdrawalRequestAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.WithdrawalRequest,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Withdrawal,
                    TargetId = withdrawalId,
                    NewValues = new Dictionary<string, object?>
                    {
                        ["status"] = WithdrawalStatus.Pending.ToString(),
                        ["amount"] = amount
                    },
                    Metadata = new Dictionary<string, object?>
                    {
                        ["walletId"] = wallet.WalletId
                    }
                };

                var walletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    FromWalletId = wallet.WalletId,
                    ToWalletId = wallet.WalletId,
                    ReferenceId = withdrawalId,
                    ReferenceType = (int)ReferenceType.Withdrawal,
                    TransactionType = (int)TransactionType.Withdrawal_Lock,
                    Amount = amount,
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = now
                };

                var availableOut = new wallet_ledger
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

                var holdIn = new wallet_ledger
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

                wallet.AvailableBalance -= amount;
                wallet.HoldBalance += amount;
                wallet.UpdatedAt = now;

                await _withdrawalRepo.AddAsync(withdrawal, ct);
                await _walletTxRepo.AddAsync(walletTx, ct);
                await _ledgerRepo.AddAsync(availableOut, ct);
                await _ledgerRepo.AddAsync(holdIn, ct);
                await _walletRepo.UpdateAsync(wallet, ct);
                await _auditService.EnqueueAsync(withdrawalRequestAuditEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<Guid>.Success(withdrawalId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _logger.LogError(ex, "Lỗi tạo yêu cầu rút tiền cho user {UserId}", userId);

                return Result<Guid>.Fail(
                    new Error("Withdrawal.CreateFailed", "Không thể tạo yêu cầu rút tiền."));
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

            var previousWithdrawalStatus = (WithdrawalStatus)withdrawalEntity.WithdrawalStatus;

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

            var approveWithdrawalAuditDiff = new AuditDiffBuilder()
                .Add(
                    "status",
                    previousWithdrawalStatus.ToString(),
                    WithdrawalStatus.Processing.ToString());

            var approveWithdrawalAuditEvent = new AuditEvent
            {
                Category = AuditCategory.Administration,
                Action = AuditActions.WithdrawalApprove,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                UserId = moderatorId,
                TargetType = AuditTargetTypes.Withdrawal,
                TargetId = withdrawalEntity.WithdrawalId,
                OldValues = approveWithdrawalAuditDiff.OldValues,
                NewValues = approveWithdrawalAuditDiff.NewValues,
                Metadata = new Dictionary<string, object?>
                {
                    ["amount"] = withdrawalEntity.Amount,
                    ["processingMode"] = "PayOS"
                }
            };

            await _withdrawalRepo.UpdateAsync(withdrawalEntity, ct);
            await _auditService.EnqueueAsync(approveWithdrawalAuditEvent, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await Task.Delay(2000, ct);
            await SyncWithdrawalStatusAsync(withdrawalId, ct);

            return Result<bool>.Success(true);
        }

        //public async Task<Result<bool>> RejectWithdrawalAsync(
        //    Guid moderatorId, Guid withdrawalId, RejectWithdrawalRequest request, CancellationToken ct = default)
        //{
        //    var validationResult = await _rejectValidator.ValidateAsync(request, ct);
        //    if (!validationResult.IsValid)
        //    {
        //        var errors = string.Join(", ", validationResult.Errors.Select(x => x.ErrorMessage));
        //        return Result<bool>.Fail(new Error("Withdrawal.InvalidRequest", errors));
        //    }

        //    var withdrawalEntity = await _withdrawalRepo.GetByIdAsync(withdrawalId, ct);
        //    if (withdrawalEntity == null)
        //        return Result<bool>.Fail(new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));

        //    if (withdrawalEntity.WithdrawalStatus != (int)WithdrawalStatus.Pending)
        //        return Result<bool>.Fail(new Error("Withdrawal.InvalidStatus", "Yêu cầu không ở trạng thái chờ duyệt."));

        //    withdrawalEntity.RejectReason = request.Reason;
        //    withdrawalEntity.ProcessedBy = moderatorId;
        //    await RevertHoldToAvailableAsync(withdrawalEntity, WithdrawalStatus.Rejected, request.Reason, ct);

        //    return Result<bool>.Success(true);
        //}

        public async Task<Result<bool>> RejectWithdrawalAsync(
            Guid moderatorId,
            Guid withdrawalId,
            RejectWithdrawalRequest request,
            CancellationToken ct = default)
        {
            var validationResult =
                await _rejectValidator.ValidateAsync(
                    request,
                    ct);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(
                    ", ",
                    validationResult.Errors
                        .Select(x => x.ErrorMessage));

                return Result<bool>.Fail(
                    new Error(
                        "Withdrawal.InvalidRequest",
                        errors));
            }

            notification? rejectedNotification = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var withdrawal =
                    await _withdrawalRepo.GetByIdForUpdateAsync(
                        withdrawalId,
                        ct);

                if (withdrawal == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<bool>.Fail(
                        new Error(
                            "Withdrawal.NotFound",
                            "Không tìm thấy yêu cầu rút tiền."));
                }

                if (withdrawal.WithdrawalStatus !=
                    (int)WithdrawalStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<bool>.Fail(
                        new Error(
                            "Withdrawal.AlreadyProcessed",
                            "Yêu cầu rút tiền đã được xử lý bởi Moderator khác."));
                }

                var previousWithdrawalStatus = (WithdrawalStatus)withdrawal.WithdrawalStatus;

                var wallet =
                    await _walletRepo.GetByIdForUpdateAsync(
                        withdrawal.WalletId,
                        ct);

                if (wallet?.UserId is not Guid recipientId)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy người sở hữu ví.");
                }

                var amount = withdrawal.Amount ?? 0;

                if (amount <= 0)
                {
                    throw new InvalidOperationException(
                        "Số tiền rút không hợp lệ.");
                }

                if (wallet.HoldBalance < amount)
                {
                    throw new InvalidOperationException(
                        "Số dư Hold không đủ để hoàn lại yêu cầu rút tiền.");
                }

                var now = DateTime.UtcNow;

                var walletTransaction =
                    new wallet_transaction
                    {
                        WalletTransactionId = Guid.NewGuid(),

                        FromWalletId = wallet.WalletId,
                        ToWalletId = wallet.WalletId,

                        ReferenceId =
                            withdrawal.WithdrawalId,

                        ReferenceType =
                            (int)ReferenceType.Withdrawal,

                        TransactionType =
                            (int)TransactionType.Withdrawal_Revert,

                        Amount = amount,

                        WalletTransactionStatus =
                            (int)WalletTransactionStatus.Completed,

                        CreatedAt = now
                    };

                var holdOut = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),

                    WalletTransactionId =
                        walletTransaction.WalletTransactionId,

                    WalletId = wallet.WalletId,

                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Hold,

                    Amount = amount,

                    BalanceBefore = wallet.HoldBalance,
                    BalanceAfter = wallet.HoldBalance - amount,

                    ReferenceType =
                        (int)ReferenceType.Withdrawal,

                    ReferenceId =
                        withdrawal.WithdrawalId,

                    Description =
                        $"Hoan tien withdrawal bi tu choi: {request.Reason}",

                    CreatedAt = now
                };

                var availableIn = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),

                    WalletTransactionId =
                        walletTransaction.WalletTransactionId,

                    WalletId = wallet.WalletId,

                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Available,

                    Amount = amount,

                    BalanceBefore = wallet.AvailableBalance,
                    BalanceAfter = wallet.AvailableBalance + amount,

                    ReferenceType =
                        (int)ReferenceType.Withdrawal,

                    ReferenceId =
                        withdrawal.WithdrawalId,

                    Description =
                        $"Hoan tien withdrawal bi tu choi: {request.Reason}",

                    CreatedAt = now
                };

                wallet.HoldBalance -= amount;
                wallet.AvailableBalance += amount;
                wallet.UpdatedAt = now;

                withdrawal.WithdrawalStatus =
                    (int)WithdrawalStatus.Rejected;

                withdrawal.RejectReason =
                    request.Reason;

                withdrawal.ProcessedBy =
                    moderatorId;

                withdrawal.ProcessedAt =
                    now;

                var rejectWithdrawalAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousWithdrawalStatus.ToString(),
                        WithdrawalStatus.Rejected.ToString());

                var rejectWithdrawalAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.WithdrawalReject,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Withdrawal,
                    TargetId = withdrawal.WithdrawalId,
                    OldValues = rejectWithdrawalAuditDiff.OldValues,
                    NewValues = rejectWithdrawalAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["amount"] = amount
                    }
                };

                await _walletTxRepo.AddAsync(
                    walletTransaction,
                    ct);

                await _ledgerRepo.AddAsync(
                    holdOut,
                    ct);

                await _ledgerRepo.AddAsync(
                    availableIn,
                    ct);

                await _walletRepo.UpdateAsync(
                    wallet,
                    ct);

                await _withdrawalRepo.UpdateAsync(
                    withdrawal,
                    ct);

                rejectedNotification =
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            recipientId,
                            "Yêu cầu rút tiền bị từ chối",
                            $"Yêu cầu rút tiền đã bị từ chối. Lý do: {request.Reason}",
                            NotificationTargetType.Withdrawal,
                            withdrawal.WithdrawalId),
                        ct);
                await _auditService.EnqueueAsync(rejectWithdrawalAuditEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);

                _logger.LogError(
                    ex,
                    "Lỗi từ chối Withdrawal {WithdrawalId}",
                    withdrawalId);

                return Result<bool>.Fail(
                    new Error(
                        "Withdrawal.RejectFailed",
                        "Không thể từ chối yêu cầu rút tiền."));
            }

            await _notificationService.PublishCreatedSafelyAsync(
                rejectedNotification);

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


        public async Task<Result<PagedResult<WithdrawalListItemDto>>> GetMyWithdrawalsAsync(
            Guid userId,
            WithdrawalSearchRequest request,
            CancellationToken ct = default)
        {
            var query = new WithdrawalQuery
            {
                UserId = userId,
                Status = request.Status,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                PrioritizeActionable = false,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _withdrawalRepo.GetPagedAsync(query, ct);

            return Result<PagedResult<WithdrawalListItemDto>>.Success(
                new PagedResult<WithdrawalListItemDto>
                {
                    Items = _mapper.Map<List<WithdrawalListItemDto>>(result.Items),
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount
                });
        }


        public async Task<Result<WithdrawalDetailDto>> GetMyWithdrawalDetailAsync(
            Guid userId,
            Guid withdrawalId,
            CancellationToken ct = default)
        {
            var withdrawal = await _withdrawalRepo.GetReadModelByIdAsync(withdrawalId, ct);

            if (withdrawal == null || withdrawal.UserId != userId)
            {
                return Result<WithdrawalDetailDto>.Fail(
                    new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));
            }

            var transactions = await _walletTxRepo.GetByReferenceAsync(
                ReferenceType.Withdrawal,
                withdrawalId,
                ct);

            var response = _mapper.Map<WithdrawalDetailDto>(withdrawal);
            response.FinancialEvents = _mapper.Map<List<WithdrawalFinancialEventDto>>(transactions);

            return Result<WithdrawalDetailDto>.Success(response);
        }

        public async Task<Result<PagedResult<ModeratorWithdrawalListItemDto>>> GetAllForModeratorAsync(
            ModeratorWithdrawalSearchRequest request,
            CancellationToken ct = default)
        {
            var query = new WithdrawalQuery
            {
                UserId = request.UserId,
                Keyword = request.Keyword,
                Status = request.Status,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                PrioritizeActionable = true,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _withdrawalRepo.GetPagedAsync(query, ct);
            var items = _mapper.Map<List<ModeratorWithdrawalListItemDto>>(result.Items);

            for (var i = 0; i < items.Count; i++)
            {
                var canProcess = result.Items[i].Status == WithdrawalStatus.Pending;

                items[i].Actions = new ModeratorWithdrawalActionsDto
                {
                    CanApprove = canProcess,
                    CanReject = canProcess
                };
            }

            return Result<PagedResult<ModeratorWithdrawalListItemDto>>.Success(
                new PagedResult<ModeratorWithdrawalListItemDto>
                {
                    Items = items,
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount
                });
        }


        public async Task<Result<ModeratorWithdrawalDetailDto>> GetDetailForModeratorAsync(
            Guid withdrawalId,
            CancellationToken ct = default)
        {
            var withdrawal = await _withdrawalRepo.GetReadModelByIdAsync(withdrawalId, ct);

            if (withdrawal == null)
            {
                return Result<ModeratorWithdrawalDetailDto>.Fail(
                    new Error("Withdrawal.NotFound", "Không tìm thấy yêu cầu rút tiền."));
            }

            var transactions = await _walletTxRepo.GetByReferenceAsync(
                ReferenceType.Withdrawal,
                withdrawalId,
                ct);

            var response = _mapper.Map<ModeratorWithdrawalDetailDto>(withdrawal);
            response.FinancialEvents = _mapper.Map<List<WithdrawalFinancialEventDto>>(transactions);

            if (withdrawal.ProcessedByUserId.HasValue)
            {
                response.ProcessedBy = new WithdrawalProcessorSummaryDto
                {
                    UserId = withdrawal.ProcessedByUserId.Value,
                    Username = withdrawal.ProcessedByUsername
                };
            }

            var canProcess = withdrawal.Status == WithdrawalStatus.Pending;

            response.Actions = new ModeratorWithdrawalActionsDto
            {
                CanApprove = canProcess,
                CanReject = canProcess
            };

            return Result<ModeratorWithdrawalDetailDto>.Success(response);
        }


        public async Task<Result<bool>> ApproveSimulatedWithdrawalAsync(
            Guid moderatorId,
            Guid withdrawalId,
            CancellationToken ct = default)
        {
            notification? completedNotification = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var withdrawal =
                    await _withdrawalRepo.GetByIdForUpdateAsync(
                        withdrawalId,
                        ct);

                if (withdrawal == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<bool>.Fail(
                        new Error(
                            "Withdrawal.NotFound",
                            "Không tìm thấy yêu cầu rút tiền."));
                }

                if (withdrawal.WithdrawalStatus !=
                    (int)WithdrawalStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return Result<bool>.Fail(
                        new Error(
                            "Withdrawal.AlreadyProcessed",
                            "Yêu cầu rút tiền đã được xử lý bởi Moderator khác."));
                }

                var previousWithdrawalStatus = (WithdrawalStatus)withdrawal.WithdrawalStatus;

                var wallet =
                    await _walletRepo.GetByIdForUpdateAsync(
                        withdrawal.WalletId,
                        ct);

                if (wallet?.UserId is not Guid recipientId)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy người sở hữu ví.");
                }

                var amount = withdrawal.Amount ?? 0;

                if (amount <= 0)
                {
                    throw new InvalidOperationException(
                        "Số tiền rút không hợp lệ.");
                }

                if (wallet.HoldBalance < amount)
                {
                    throw new InvalidOperationException(
                        "Số dư Hold không đủ để hoàn tất yêu cầu rút tiền.");
                }

                var now = DateTime.UtcNow;

                var walletTransaction = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),

                    FromWalletId = wallet.WalletId,
                    ToWalletId = null,

                    ReferenceId = withdrawal.WithdrawalId,
                    ReferenceType = (int)ReferenceType.Withdrawal,

                    TransactionType =
                        (int)TransactionType.Withdrawal_Success,

                    Amount = -amount,

                    WalletTransactionStatus =
                        (int)WalletTransactionStatus.Completed,

                    CreatedAt = now
                };

                var ledger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),

                    WalletTransactionId =
                        walletTransaction.WalletTransactionId,

                    WalletId = wallet.WalletId,

                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Hold,

                    Amount = amount,

                    BalanceBefore = wallet.HoldBalance,
                    BalanceAfter = wallet.HoldBalance - amount,

                    ReferenceType =
                        (int)ReferenceType.Withdrawal,

                    ReferenceId =
                        withdrawal.WithdrawalId,

                    Description =
                        $"Hoan tat yeu cau rut tien {withdrawal.WithdrawalId}",

                    CreatedAt = now
                };

                wallet.HoldBalance -= amount;
                wallet.UpdatedAt = now;

                withdrawal.WithdrawalStatus =
                    (int)WithdrawalStatus.Completed;

                withdrawal.ProcessedBy = moderatorId;
                withdrawal.ProcessedAt = now;

                var approveWithdrawalAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousWithdrawalStatus.ToString(),
                        WithdrawalStatus.Completed.ToString());

                var approveWithdrawalAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.WithdrawalApprove,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = moderatorId,
                    TargetType = AuditTargetTypes.Withdrawal,
                    TargetId = withdrawal.WithdrawalId,
                    OldValues = approveWithdrawalAuditDiff.OldValues,
                    NewValues = approveWithdrawalAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["amount"] = amount,
                        ["processingMode"] = "Simulated"
                    }
                };

                await _walletTxRepo.AddAsync(
                    walletTransaction,
                    ct);

                await _ledgerRepo.AddAsync(
                    ledger,
                    ct);

                await _walletRepo.UpdateAsync(
                    wallet,
                    ct);

                await _withdrawalRepo.UpdateAsync(
                    withdrawal,
                    ct);

                completedNotification =
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            recipientId,
                            "Yêu cầu rút tiền đã hoàn tất",
                            "Yêu cầu rút tiền của bạn đã được xử lý hoàn tất trên hệ thống.",
                            NotificationTargetType.Withdrawal,
                            withdrawal.WithdrawalId),
                        ct);
                await _auditService.EnqueueAsync(approveWithdrawalAuditEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);

                _logger.LogError(
                    ex,
                    "Lỗi xử lý simulated withdrawal {WithdrawalId}",
                    withdrawalId);

                return Result<bool>.Fail(
                    new Error(
                        "Withdrawal.CompleteFailed",
                        "Không thể hoàn tất yêu cầu rút tiền."));
            }

            await _notificationService.PublishCreatedSafelyAsync(
                completedNotification);

            return Result<bool>.Success(true);
        }

        public async Task<Result<WithdrawalQuotaResponseDto>> GetMyWithdrawalQuotaAsync(Guid userId, CancellationToken ct = default)
        {
            var policy = await _platformPolicyProvider.GetWithdrawalConfigAsync(ct);
            var window = GetVietnamDayWindow(_clock.GetUtcNow());
            var usage = await _withdrawalRepo.GetDailyUsageAsync(userId, window.FromUtc, window.ToUtc, ct);
            var used = usage.CompletedAmount + usage.ReservedAmount;

            return Result<WithdrawalQuotaResponseDto>.Success(new WithdrawalQuotaResponseDto
            {
                MinimumWithdrawalAmount = policy.MinimumWithdrawalAmount,
                MaximumWithdrawalAmount = policy.MaximumWithdrawalAmount,
                DailyWithdrawalLimit = policy.DailyWithdrawalLimit,
                CompletedTodayAmount = usage.CompletedAmount,
                ActiveReservedAmount = usage.ReservedAmount,
                UsedDailyLimitAmount = used,
                RemainingDailyLimitAmount = Math.Max(policy.DailyWithdrawalLimit - used, 0m),
                DailyWithdrawalCountLimit = policy.DailyWithdrawalCountLimit,
                UsedDailyWithdrawalCount = usage.UsedCount,
                RemainingDailyWithdrawalCount = Math.Max(policy.DailyWithdrawalCountLimit - usage.UsedCount, 0),
                ResetAt = window.ResetAt
            });
        }


        // ================== HELPERS DÙNG CHUNG ==================

        private static (DateTime FromUtc, DateTime ToUtc, DateTimeOffset ResetAt) GetVietnamDayWindow(DateTimeOffset nowUtc)
        {
            var localNow = nowUtc.ToOffset(VietnamOffset);
            var startLocal = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, VietnamOffset);
            var resetAt = startLocal.AddDays(1);

            return (startLocal.UtcDateTime, resetAt.UtcDateTime, resetAt);
        }

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

                var previousWithdrawalStatus = (WithdrawalStatus)lockedWithdrawal.WithdrawalStatus;

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

                var completeWithdrawalAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousWithdrawalStatus.ToString(),
                        WithdrawalStatus.Completed.ToString());

                var completeWithdrawalAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.WithdrawalComplete,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.ExternalSystem,
                    Source = AuditSource.Internal,
                    TargetType = AuditTargetTypes.Withdrawal,
                    TargetId = lockedWithdrawal.WithdrawalId,
                    OldValues = completeWithdrawalAuditDiff.OldValues,
                    NewValues = completeWithdrawalAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["amount"] = amount,
                        ["processingMode"] = "PayOS"
                    }
                };

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
                await _auditService.EnqueueAsync(completeWithdrawalAuditEvent, ct);

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

                var previousWithdrawalStatus = (WithdrawalStatus)lockedWithdrawal.WithdrawalStatus;

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

                var revertWithdrawalAuditDiff = new AuditDiffBuilder()
                    .Add(
                        "status",
                        previousWithdrawalStatus.ToString(),
                        finalStatus.ToString());

                var revertWithdrawalAuditEvent = new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.WithdrawalRevert,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.ExternalSystem,
                    Source = AuditSource.Internal,
                    TargetType = AuditTargetTypes.Withdrawal,
                    TargetId = lockedWithdrawal.WithdrawalId,
                    OldValues = revertWithdrawalAuditDiff.OldValues,
                    NewValues = revertWithdrawalAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["amount"] = amount
                    }
                };

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
                await _auditService.EnqueueAsync(revertWithdrawalAuditEvent, ct);

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
