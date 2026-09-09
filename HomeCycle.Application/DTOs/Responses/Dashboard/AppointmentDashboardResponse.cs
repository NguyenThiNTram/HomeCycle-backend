namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class AppointmentDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalAppointments { get; init; }
    public int UpcomingCount { get; init; }
    public int TodayCount { get; init; }
    public int PendingCount { get; init; }
    public int CompletedInPeriodCount { get; init; }
    public int CancelledInPeriodCount { get; init; }
    public int ExpiredCount { get; init; }
    public int RescheduleProposalCount { get; init; }
    public int OverdueCount { get; init; }
    public decimal? AverageOverdueAgeHours { get; init; }
    public decimal? OldestOverdueAgeHours { get; init; }
    public IReadOnlyList<DistributionItem> CurrentStatusDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> AppointmentTypeDistribution { get; init; } = [];
    public IReadOnlyList<TimeSeriesPoint> ScheduledSeries { get; init; } = [];
    public IReadOnlyList<AgingBucket> OverdueAgingDistribution { get; init; } = [];
}
