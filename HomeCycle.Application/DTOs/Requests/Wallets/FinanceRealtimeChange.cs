using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Wallets
{
    public sealed class FinanceRealtimeChange
    {
        public FinanceEventType EventType { get; init; }
        public Guid? UserId { get; init; }
        public IReadOnlyCollection<Guid> AffectedUserIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> WalletTransactionIds { get; init; } = Array.Empty<Guid>();

        public ReferenceType? ReferenceType { get; init; }
        public Guid? ReferenceId { get; init; }
        public TransactionType? TransactionType { get; init; }

        public Guid? PaymentId { get; init; }
        public Guid? WithdrawalId { get; init; }
        public DateTime OccurredAt { get; init; }
    }
}
