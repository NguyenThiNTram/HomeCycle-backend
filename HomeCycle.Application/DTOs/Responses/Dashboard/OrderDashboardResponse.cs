namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class OrderDashboardResponse
{
    public int CreatedInPeriodCount { get; init; }
    public int SuccessfulInPeriodCount { get; init; }
    public int SuccessfulOrdersMissingAmountCount { get; init; }
    public decimal Gmv { get; init; }
    public decimal? AverageOrderValue { get; init; }
    public decimal? CancellationReturnRate { get; init; }
    public decimal? CompletionRate { get; init; }
    public string GmvBasis => "CurrentCompletedOrders.CompletedAt.FinalTotalAmount.IncludesConfiguredShipping";
    public string RateBasis => "CurrentStatusOfOrdersCreatedInPeriod";
    public string PaymentMethodBasis => "PaidPaymentEventsInPeriod.DepositAndFullPayment.IncludesSubsequentRefunds";
    public IReadOnlyList<DistributionItem> CreatedStatusDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> DeliveryMethodDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> PaymentMethodDistribution { get; init; } = [];
    public IReadOnlyList<OrderTradePoint> TradeSeries { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalOrders { get; init; }
    public int ActiveOrderCount { get; init; }
    public int CompletedInPeriodCount { get; init; }
    public int CancelledInPeriodCount { get; init; }
    public int ReturnedInPeriodCount { get; init; }
    public decimal? AverageActiveOrderAgeHours { get; init; }
    public decimal? OldestActiveOrderAgeHours { get; init; }
    public IReadOnlyList<DistributionItem> CurrentStatusDistribution { get; init; } = [];
    public IReadOnlyList<AgingBucket> ActiveOrderAgingDistribution { get; init; } = [];
    public IReadOnlyList<OrderOutcomeSeriesPoint> OutcomeSeries { get; init; } = [];
}

public sealed record OrderTradePoint(DateOnly From, DateOnly ToExclusive, int CreatedCount, int CompletedCount, decimal Gmv);

public sealed record OrderOutcomeSeriesPoint(
    DateOnly From,
    DateOnly ToExclusive,
    int CompletedCount,
    int CancelledCount,
    int ReturnedCount);
