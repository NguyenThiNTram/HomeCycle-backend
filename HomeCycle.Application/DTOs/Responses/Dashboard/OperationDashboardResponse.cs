namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class OperationOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public OrderOverviewMetric Orders { get; init; } = new();
    public AppointmentOverviewMetric Appointments { get; init; } = new();
    public PaymentOverviewMetric Payments { get; init; } = new();
    public DisputeOverviewMetric Disputes { get; init; } = new();
}

public sealed class OrderOverviewMetric
{
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
}

public sealed class AppointmentOverviewMetric
{
    public int UpcomingCount { get; init; }
    public int TodayCount { get; init; }
}

public sealed class PaymentOverviewMetric
{
    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
}

public sealed class DisputeOverviewMetric
{
    public int TotalCount { get; init; }
    public int UnresolvedCount { get; init; }
    public int ResolvedInPeriodCount { get; init; }
}
