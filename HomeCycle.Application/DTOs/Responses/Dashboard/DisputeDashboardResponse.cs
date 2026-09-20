namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class DisputeDashboardResponse
{
    public int OpenedInPeriodCount { get; init; }
    public int OrdersCreatedInPeriodCount { get; init; }
    public int DisputedOrdersCreatedInPeriodCount { get; init; }
    public decimal? OrderDisputeRate { get; init; }
    public string DisputeRateBasis => "DistinctOrdersCreatedInPeriodWithMatchingOrderDisputes/OrdersCreatedInPeriod";
    public decimal CurrentDisputedHeldAmount { get; init; }
    public string HeldAmountBasis => "CurrentPositiveNetCompletedHoldLedgerByDistinctUnresolvedOrder";
    public IReadOnlyList<DistributionItem> ResolutionDistribution { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalDisputes { get; init; }
    public int UnresolvedDisputeCount { get; init; }
    public int ResolvedInPeriodCount { get; init; }
    public decimal? AverageResolutionTimeHours { get; init; }
    public decimal? OldestUnresolvedAgeHours { get; init; }
    public IReadOnlyList<DistributionItem> CurrentStatusDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> CategoryDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> UnresolvedByCategory { get; init; } = [];
    public IReadOnlyList<AgingBucket> UnresolvedAgingDistribution { get; init; } = [];
    public IReadOnlyList<DisputeFlowSeriesPoint> OpenedVsResolvedSeries { get; init; } = [];
    public int UnknownCategoryCount { get; init; }
}

public sealed record DisputeFlowSeriesPoint(
    DateOnly From,
    DateOnly ToExclusive,
    int OpenedCount,
    int ResolvedCount);
