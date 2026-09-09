using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Dashboard
{
    public sealed class FinanceOverviewResponse
    {
        public DateTime GeneratedAtUtc { get; init; }

        public DashboardPeriod Period { get; init; } = new();

        public FinancePositionMetrics Position { get; init; } = null!;

        public FinancePeriodActivityMetrics Activity { get; init; } = null!;
    }

    public sealed record FinancePositionMetrics(
        decimal TotalRecordedWalletBalance,
        decimal SystemWalletBalance,
        decimal SystemWalletAvailableBalance,
        decimal SystemWalletHoldBalance,
        decimal UserAvailableFunds,
        decimal UserFundsHeld,
        decimal OrderEscrowHeld,
        decimal WithdrawalLocked,
        decimal ShippingEscrowBalance,
        decimal CurrentPendingPaymentAmount);

    public sealed record FinancePeriodActivityMetrics(
        decimal ExternalInflow,
        decimal ExternalOutflow,
        decimal NetExternalCashFlow,
        decimal ProcessedPaymentAmount,
        decimal RefundedAmount,
        decimal CreatedFailedPaymentAmount);

    public sealed class FinanceCashFlowResponse
    {
        public DateTime GeneratedAtUtc { get; init; }

        public DashboardPeriod Period { get; init; } = new();

        public FinanceCashFlowTotals Totals { get; init; } = null!;

        public IReadOnlyList<FinanceCashFlowPoint> Series { get; init; } = [];

        public IReadOnlyList<FinanceAmountBreakdownItem> InflowSources { get; init; } = [];

        public IReadOnlyList<FinanceAmountBreakdownItem> InternalMovements { get; init; } = [];
    }

    public sealed record FinanceCashFlowTotals(
        decimal ExternalInflow,
        decimal ExternalOutflow,
        decimal NetExternalCashFlow);

    public sealed record FinanceCashFlowPoint(
        DateOnly From,
        DateOnly ToExclusive,
        decimal Inflow,
        decimal Outflow,
        decimal Net);

    public sealed record FinanceAmountBreakdownItem(
        string Key,
        string Label,
        decimal Amount,
        decimal Percentage);

    public sealed class FinancePaymentStatusResponse
    {
        public DateTime GeneratedAtUtc { get; init; }

        public DashboardPeriod Period { get; init; } = new();

        public int TotalCreatedCount { get; init; }

        public decimal TotalCreatedAmount { get; init; }

        public IReadOnlyList<FinancePaymentStatusItem> Statuses { get; init; } = [];

        public decimal PaidRatePercent { get; init; }

        public decimal FailureRatePercent { get; init; }

        public decimal RefundedPaymentRatePercent { get; init; }
    }

    public sealed record FinancePaymentStatusItem(
        PaymentStatus? Status,
        string Label,
        int Count,
        decimal Amount,
        decimal PercentageOfCreated);

    public sealed class FinanceHealthResponse
    {
        public DateTime GeneratedAtUtc { get; init; }

        public DashboardPeriod Period { get; init; } = new();

        public FinanceCountAmountMetric StalePendingPayments { get; init; } = new(0, 0);

        public FinanceCountAmountMetric PendingPaymentsWithoutExpiry { get; init; } = new(0, 0);

        public FinanceCountAmountMetric PendingWithdrawals { get; init; } = new(0, 0);

        public FinanceCountAmountMetric ProcessingWithdrawals { get; init; } = new(0, 0);

        public FinanceCountAmountMetric OverdueReleaseOrders { get; init; } = new(0, 0);

        public FinanceCountAmountMetric CompletedOrdersMissingReleaseDeadline { get; init; } = new(0, 0);

        public FinanceCountAmountMetric ActiveDisputeHeldFunds { get; init; } = new(0, 0);

        public int NegativeWalletCount { get; init; }

        public FinanceCountAmountMetric UnclassifiedTransactionsInPeriod { get; init; } = new(0, 0);
    }

    public sealed record FinanceCountAmountMetric(
        int Count,
        decimal Amount);

    public sealed class FinanceTransactionItem
    {
        public Guid WalletTransactionId { get; init; }

        public DateTime CreatedAt { get; init; }

        public TransactionType? TransactionType { get; init; }

        public string TransactionLabel { get; init; } = string.Empty;

        public ReferenceType? ReferenceType { get; init; }

        public Guid? ReferenceId { get; init; }

        public string? ReferenceCode { get; init; }

        public Guid? PaymentId { get; init; }

        public PaymentMethod? PaymentMethod { get; init; }

        public Guid? UserId { get; init; }

        public string? Username { get; init; }

        public decimal Amount { get; init; }

        public FinanceFlowScope FlowScope { get; init; }

        public WalletTransactionStatus? Status { get; init; }
    }


    // ==========================================================
    // Repository projections - không trả trực tiếp qua endpoint
    // ==========================================================

    public sealed class FinanceOverviewData
    {
        public decimal TotalRecordedWalletBalance { get; set; }

        public decimal SystemWalletAvailableBalance { get; set; }

        public decimal SystemWalletHoldBalance { get; set; }

        public decimal UserAvailableFunds { get; set; }

        public decimal UserFundsHeld { get; set; }

        public decimal OrderEscrowHeld { get; set; }

        public decimal WithdrawalLocked { get; set; }

        public decimal ShippingEscrowBalance { get; set; }

        public decimal CurrentPendingPaymentAmount { get; set; }

        public decimal ExternalInflow { get; set; }

        public decimal ExternalOutflow { get; set; }

        public decimal ProcessedPaymentAmount { get; set; }

        public decimal RefundedAmount { get; set; }

        public decimal CreatedFailedPaymentAmount { get; set; }
    }

    public sealed record FinanceDailyAmount(
        DateTime Date,
        decimal Amount);

    public sealed record FinanceTypeAmountRow(
        int? TransactionType,
        decimal Amount);

    public sealed class FinanceCashFlowData
    {
        public decimal ExternalInflow { get; set; }

        public decimal ExternalOutflow { get; set; }

        public decimal DepositPayOsAmount { get; set; }

        public decimal FullPayOsAmount { get; set; }

        public decimal SubscriptionPayOsAmount { get; set; }

        public decimal PayOsGhnShippingCollectedAmount { get; set; }

        public List<FinanceDailyAmount> InflowDaily { get; set; } = [];

        public List<FinanceDailyAmount> OutflowDaily { get; set; } = [];

        public List<FinanceTypeAmountRow> InternalMovements { get; set; } = [];
    }

    public sealed record FinanceCodeAmountRow(
        int? Code,
        int Count,
        decimal Amount);

    public sealed class FinancePaymentStatusData
    {
        public List<FinanceCodeAmountRow> Statuses { get; set; } = [];
    }

    public sealed class FinanceHealthData
    {
        public FinanceCountAmountMetric StalePendingPayments { get; set; } = new(0, 0);

        public FinanceCountAmountMetric PendingPaymentsWithoutExpiry { get; set; } = new(0, 0);

        public FinanceCountAmountMetric PendingWithdrawals { get; set; } = new(0, 0);

        public FinanceCountAmountMetric ProcessingWithdrawals { get; set; } = new(0, 0);

        public FinanceCountAmountMetric OverdueReleaseOrders { get; set; } = new(0, 0);

        public FinanceCountAmountMetric CompletedOrdersMissingReleaseDeadline { get; set; } = new(0, 0);

        public FinanceCountAmountMetric ActiveDisputeHeldFunds { get; set; } = new(0, 0);

        public int NegativeWalletCount { get; set; }

        public FinanceCountAmountMetric UnclassifiedTransactionsInPeriod { get; set; } = new(0, 0);
    }
}
