using AutoMapper;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
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
    public sealed class FinanceRealtimeService : IFinanceRealtimeService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _walletTxRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWithdrawalRepository _withdrawalRepo;
        private readonly IFinanceRealtimePublisher _publisher;
        private readonly IMapper _mapper;
        private readonly ILogger<FinanceRealtimeService> _logger;

        public FinanceRealtimeService(
            IWalletRepository walletRepo,
            IWalletTransactionRepository walletTxRepo,
            IPaymentRepository paymentRepo,
            IWithdrawalRepository withdrawalRepo,
            IFinanceRealtimePublisher publisher,
            IMapper mapper,
            ILogger<FinanceRealtimeService> logger)
        {
            _walletRepo = walletRepo;
            _walletTxRepo = walletTxRepo;
            _paymentRepo = paymentRepo;
            _withdrawalRepo = withdrawalRepo;
            _publisher = publisher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task PublishUpdatedSafelyAsync(FinanceRealtimeChange change)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await PublishCoreAsync(change, timeout.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể phát FinanceUpdated {EventType}.",
                    change.EventType);
            }
        }

        private async Task PublishCoreAsync(
            FinanceRealtimeChange change,
            CancellationToken ct)
        {
            var transactionIds = change.WalletTransactionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (transactionIds.Count == 0 &&
                change.ReferenceType.HasValue &&
                change.ReferenceId.HasValue &&
                change.TransactionType.HasValue)
            {
                var referencedTransactions = await _walletTxRepo.GetByReferenceAsync(
                    change.ReferenceType.Value,
                    change.ReferenceId.Value,
                    ct);

                var latest = referencedTransactions
                    .Where(x =>
                        x.TransactionType == (int)change.TransactionType.Value &&
                        x.WalletTransactionStatus == (int)WalletTransactionStatus.Completed)
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.WalletTransactionId)
                    .FirstOrDefault();

                if (latest != null)
                    transactionIds.Add(latest.WalletTransactionId);
            }

            var financeTransactions = transactionIds.Count == 0
                ? Array.Empty<WalletTransactionListItemDto>()
                : await _walletTxRepo.GetListByIdsAsync(transactionIds, ct);

            var affectedWalletIds = financeTransactions
                .SelectMany(x => new[] { x.FromWalletId, x.ToWalletId })
                .Where(x => x.HasValue)
                .Select(x => x.GetValueOrDefault())
                .Distinct()
                .ToArray();

            var wallets = new List<wallet>();

            foreach (var walletId in affectedWalletIds)
            {
                var wallet = await _walletRepo.GetByIdAsync(walletId, ct);

                if (wallet != null)
                    wallets.Add(wallet);
            }

            var systemWalletDtos = wallets
                .Where(x => x.UserId == null)
                .Select(MapWallet)
                .ToList();

            var paymentId = change.PaymentId ??
                financeTransactions
                    .Select(x => x.PaymentId)
                    .FirstOrDefault(x => x.HasValue);

            var payment = paymentId.HasValue
                ? await _paymentRepo.GetByIdAsync(paymentId.Value, ct)
                : null;

            var paymentDto = MapPayment(payment);

            var managementPaymentDto = paymentId.HasValue
                ? await _paymentRepo.GetManagementItemByIdAsync(
                    paymentId.Value,
                    ct)
                : null;

            var withdrawal = change.WithdrawalId.HasValue
                ? await _withdrawalRepo.GetReadModelByIdAsync(change.WithdrawalId.Value, ct)
                : null;

            var withdrawalDto = withdrawal == null
                ? null
                : _mapper.Map<WithdrawalListItemDto>(withdrawal);

            var eventId = Guid.NewGuid();
            var publishTasks = new List<Task>();

            if (financeTransactions.Count > 0 ||
                managementPaymentDto != null ||
                withdrawalDto != null ||
                systemWalletDtos.Count > 0)
            {
                publishTasks.Add(
                    _publisher.PublishToFinanceGroupAsync(
                        new FinanceUpdatedResponse
                        {
                            EventId = eventId,
                            EventType = change.EventType,
                            Wallets = systemWalletDtos,
                            FinanceTransactions = financeTransactions,
                            ManagementPayment = managementPaymentDto,
                            Withdrawal = withdrawalDto,
                            OccurredAt = change.OccurredAt
                        },
                        ct));
            }

            var userId = change.UserId ?? withdrawal?.UserId ?? payment?.PayerId;

            if (userId.HasValue)
            {
                var userWallets = wallets
                    .Where(x => x.UserId == userId.Value)
                    .ToList();

                var userTransactions = new List<UserWalletTransactionListItemDto>();

                foreach (var wallet in userWallets)
                {
                    userTransactions.AddRange(
                        await _walletTxRepo.GetListByWalletIdAndIdsAsync(
                            wallet.WalletId,
                            transactionIds,
                            ct));
                }

                var userTransactionDtos = userTransactions
                    .GroupBy(x => x.WalletTransactionId)
                    .Select(x => x.First())
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.WalletTransactionId)
                    .ToList();

                var userWalletDtos = userWallets
                    .Select(MapWallet)
                    .ToList();

                var userPayment = payment?.PayerId == userId.Value
                    ? paymentDto
                    : null;

                var userWithdrawal = withdrawal?.UserId == userId.Value
                    ? withdrawalDto
                    : null;

                if (userWalletDtos.Count > 0 ||
                    userTransactionDtos.Count > 0 ||
                    userPayment != null ||
                    userWithdrawal != null)
                {
                    publishTasks.Add(_publisher.PublishToUserAsync(
                        userId.Value,
                        new FinanceUpdatedResponse
                        {
                            EventId = eventId,
                            EventType = change.EventType,
                            Wallets = userWalletDtos,
                            UserTransactions = userTransactionDtos,
                            Payment = userPayment,
                            Withdrawal = userWithdrawal,
                            OccurredAt = change.OccurredAt
                        },
                        ct));
                }
            }

            if (publishTasks.Count > 0)
                await Task.WhenAll(publishTasks);
        }

        private static WalletInfoDto MapWallet(wallet wallet)
        {
            return new WalletInfoDto
            {
                WalletId = wallet.WalletId,
                WalletType = (WalletTypeEnum)wallet.WalletType,
                AvailableBalance = wallet.AvailableBalance,
                HoldBalance = wallet.HoldBalance,
                Purpose = wallet.Purpose.HasValue
                    ? (SystemWalletPurpose?)wallet.Purpose.Value
                    : null
            };
        }

        private static PaymentHistoryResponseDto? MapPayment(payment? payment)
        {
            if (payment == null ||
                !payment.Amount.HasValue ||
                !payment.PaymentMethod.HasValue ||
                !payment.PaymentStatus.HasValue)
            {
                return null;
            }

            return new PaymentHistoryResponseDto
            {
                PaymentId = payment.PaymentId,
                CreatedAt = payment.CreatedAt,
                Description = payment.Description ?? string.Empty,
                Amount = payment.Amount.Value,
                PaymentMethod = (PaymentMethod)payment.PaymentMethod.Value,
                PaymentStatus = (PaymentStatus)payment.PaymentStatus.Value,
                AgreementId = payment.AgreementId,
                OrderId = payment.OrderId,
                SubscriptionId = payment.SubscriptionId
            };
        }
    }
}
