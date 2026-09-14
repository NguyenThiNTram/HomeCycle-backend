namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class OrderDashboardResponse
{
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

public sealed record OrderOutcomeSeriesPoint(
    DateOnly From,
    DateOnly ToExclusive,
    int CompletedCount,
    int CancelledCount,
    int ReturnedCount);
