using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public sealed class FinanceUpdatedResponse
    {
        public Guid EventId { get; init; }
        public FinanceEventType EventType { get; init; }

        public IReadOnlyList<WalletInfoDto> Wallets { get; init; } = Array.Empty<WalletInfoDto>();
        public IReadOnlyList<UserWalletTransactionListItemDto> UserTransactions { get; init; } = Array.Empty<UserWalletTransactionListItemDto>();
        public IReadOnlyList<WalletTransactionListItemDto> FinanceTransactions { get; init; } = Array.Empty<WalletTransactionListItemDto>();

        public PaymentHistoryResponseDto? Payment { get; init; }
        public PaymentManagementListItemDto? ManagementPayment { get; init; }
        public WithdrawalListItemDto? Withdrawal { get; init; }

        public DateTime OccurredAt { get; init; }
    }
}
