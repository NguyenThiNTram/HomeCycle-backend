namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class AppointmentDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();

    // Current monitoring: effective appointments only.
    public int TotalAppointments { get; init; }
    public int TodayCount { get; init; }
    public int UpcomingCount { get; init; }
    public int OverdueCount { get; init; }
    public int RescheduleProposalCount { get; init; }
    public IReadOnlyList<DistributionItem> CurrentStatusDistribution { get; init; } = [];

    // Composition in the selected period, based on the actual scheduled time.
    public IReadOnlyList<DistributionItem> AppointmentTypeDistribution { get; init; } = [];
    public IReadOnlyList<AppointmentTypeSeriesPoint> AppointmentTypeSeries { get; init; } = [];

    // Finalized outcomes in the selected period.
    public AppointmentOutcomeSummary Outcome { get; init; } = new();
    public IReadOnlyList<AppointmentOutcomeByTypeItem> OutcomeByType { get; init; } = [];

    // Attendance analytics for eligible effective Inspection appointments in the selected period.
    public InspectionCheckInSummary InspectionCheckIn { get; init; } = new();
    public IReadOnlyList<CheckInParticipantItem> CheckInByParticipant { get; init; } = [];
}

public sealed record AppointmentTypeSeriesPoint(
    DateOnly From,
    DateOnly ToExclusive,
    int InspectionCount,
    int CollectionCount);

public sealed class AppointmentOutcomeSummary
{
    public int FinalizedCount { get; init; }
    public int SuccessfulCount { get; init; }
    public int FailedCount { get; init; }
    public decimal SuccessRate { get; init; }
    public decimal FailureRate { get; init; }
}

public sealed class AppointmentOutcomeByTypeItem
{
    public string AppointmentType { get; init; } = "";
    public int FinalizedCount { get; init; }
    public int SuccessfulCount { get; init; }
    public int FailedCount { get; init; }
    public decimal SuccessRate { get; init; }
    public decimal FailureRate { get; init; }
}

public sealed class InspectionCheckInSummary
{
    public int EligibleInspectionCount { get; init; }
    public int ExpectedParticipantCheckIns { get; init; }
    public int SuccessfulParticipantCheckIns { get; init; }
    public decimal ParticipantCheckInRate { get; init; }
    public int FullyCheckedInAppointmentCount { get; init; }
    public int PartialCheckInAppointmentCount { get; init; }
    public int NoCheckInAppointmentCount { get; init; }
    public decimal FullCheckInRate { get; init; }
}

public sealed class CheckInParticipantItem
{
    public string ParticipantType { get; init; } = "";
    public int EligibleCount { get; init; }
    public int CheckedInCount { get; init; }
    public int MissingCount { get; init; }
    public decimal CheckInRate { get; init; }
}
