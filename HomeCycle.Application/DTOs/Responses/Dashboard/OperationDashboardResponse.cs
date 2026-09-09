namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class OperationOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public DashboardCountMetric Payments { get; init; } = null!;
    public DashboardCountMetric Orders { get; init; } = null!;
    public DashboardCountMetric Appointments { get; init; } = null!;
    public DashboardCountMetric Disputes { get; init; } = null!;
    public IReadOnlyList<DistributionItem> DisputeCurrentStatusCounts { get; init; } = [];
}
public class OperationMetricResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalCount { get; init; }
    public CountTrend CreatedTrend { get; init; } = null!;
    public IReadOnlyList<TimeSeriesPoint> CreatedSeries { get; init; } = [];
    public IReadOnlyList<DistributionItem> CreatedInPeriodByCurrentStatus { get; init; } = [];
}
public sealed class PaymentDashboardResponse : OperationMetricResponse
{
    public IReadOnlyList<DistributionItem> CreatedInPeriodByMethod { get; init; } = [];
    public int PaidInPeriodCount { get; init; }
}
public sealed class OrderDashboardResponse : OperationMetricResponse
{
    public int CompletedInPeriodCount { get; init; }
    public int CancelledInPeriodCount { get; init; }
    public int ReturnedInPeriodCount { get; init; }
}
public sealed class AppointmentDashboardResponse : OperationMetricResponse
{
    public IReadOnlyList<DistributionItem> CreatedInPeriodByType { get; init; } = [];
    public int RescheduleProposalsCreatedInPeriodCount { get; init; }
    public int ScheduledDateInPeriodCount { get; init; }
}
