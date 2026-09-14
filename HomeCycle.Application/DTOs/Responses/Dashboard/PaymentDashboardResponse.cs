namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class PaymentDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalPayments { get; init; }
    public int PendingCount { get; init; }
    public int PaidInPeriodCount { get; init; }
    public decimal? AveragePendingAgeHours { get; init; }
    public decimal? OldestPendingAgeHours { get; init; }
    public IReadOnlyList<DistributionItem> CurrentStatusDistribution { get; init; } = [];
    public IReadOnlyList<PaymentMethodPerformanceItem> PaymentMethodPerformance { get; init; } = [];
    public IReadOnlyList<AgingBucket> PendingAgingDistribution { get; init; } = [];
    public IReadOnlyList<TimeSeriesPoint> PaidSeries { get; init; } = [];
}

public sealed record PaymentMethodPerformanceItem(
    string Method,
    int TotalCount,
    int PaidCount,
    int FailedCount,
    decimal? SuccessRate);
