using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Wallets
{
    public class WithdrawalUserSummaryDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class WithdrawalBankAccountDto
    {
        public Guid UserBankId { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public VerifyStatus? VerifyStatus { get; set; }
    }

    public class WithdrawalProcessorSummaryDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
    }

    public class WithdrawalFinancialEventDto
    {
        public Guid WalletTransactionId { get; set; }
        public TransactionType? TransactionType { get; set; }
        public WalletTransactionStatus? Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WithdrawalListItemDto
    {
        public Guid WithdrawalId { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus? Status { get; set; }
        public string? BankName { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? RejectReason { get; set; }
    }

    public class WithdrawalDetailDto
    {
        public Guid WithdrawalId { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus? Status { get; set; }
        public WithdrawalBankAccountDto BankAccount { get; set; } = new();
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? RejectReason { get; set; }
        public IReadOnlyList<WithdrawalFinancialEventDto> FinancialEvents { get; set; }
            = Array.Empty<WithdrawalFinancialEventDto>();
    }

    public class ModeratorWithdrawalActionsDto
    {
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
    }

    public class ModeratorWithdrawalListItemDto
    {
        public Guid WithdrawalId { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus? Status { get; set; }
        public WithdrawalUserSummaryDto User { get; set; } = new();
        public string? BankName { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? RejectReason { get; set; }
        public ModeratorWithdrawalActionsDto Actions { get; set; } = new();
    }

    public class ModeratorWithdrawalDetailDto
    {
        public Guid WithdrawalId { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus? Status { get; set; }
        public WithdrawalUserSummaryDto User { get; set; } = new();
        public WithdrawalBankAccountDto BankAccount { get; set; } = new();
        public WithdrawalProcessorSummaryDto? ProcessedBy { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? RejectReason { get; set; }
        public IReadOnlyList<WithdrawalFinancialEventDto> FinancialEvents { get; set; }
            = Array.Empty<WithdrawalFinancialEventDto>();
        public ModeratorWithdrawalActionsDto Actions { get; set; } = new();
    }

    public sealed class WithdrawalReadModel
    {
        public Guid WithdrawalId { get; init; }
        public Guid WalletId { get; init; }

        public Guid UserId { get; init; }
        public string? Username { get; init; }
        public string? Email { get; init; }
        public string? AvatarUrl { get; init; }

        public Guid UserBankId { get; init; }
        public string? BankCode { get; init; }
        public string? BankName { get; init; }
        public string? AccountNumber { get; init; }
        public string? AccountName { get; init; }
        public VerifyStatus? BankVerifyStatus { get; init; }

        public decimal Amount { get; init; }
        public WithdrawalStatus? Status { get; init; }
        public DateTime? RequestedAt { get; init; }
        public DateTime? ProcessedAt { get; init; }

        public Guid? ProcessedByUserId { get; init; }
        public string? ProcessedByUsername { get; init; }
        public string? RejectReason { get; init; }
    }
}
