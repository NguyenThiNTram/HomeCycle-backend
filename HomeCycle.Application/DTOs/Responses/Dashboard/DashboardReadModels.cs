namespace HomeCycle.Application.DTOs.Responses.Dashboard;

// Read projections shared between DashboardRepository and DashboardService.
// These types are not EF entities and are never persisted.
public sealed record DashboardDailyCount(DateTime Date, int Count);
public sealed record DashboardCodeCount(int? Code, int Count);

public sealed class DashboardAgingData
{
    public int UnderOneDayCount { get; set; }
    public int OneToThreeDaysCount { get; set; }
    public int ThreeToSevenDaysCount { get; set; }
    public int OverSevenDaysCount { get; set; }
    public double? AverageAgeHours { get; set; }
    public double? OldestAgeHours { get; set; }
}

public sealed class AppointmentDashboardData
{
    public int TotalAppointments { get; set; }
    public int UpcomingCount { get; set; }
    public int TodayCount { get; set; }
    public int PendingCount { get; set; }
    public int CompletedInPeriodCount { get; set; }
    public int CancelledInPeriodCount { get; set; }
    public int ExpiredCount { get; set; }
    public int RescheduleProposalCount { get; set; }
    public int OverdueCount { get; set; }
    public List<DashboardCodeCount> CurrentStatuses { get; set; } = [];
    public List<DashboardCodeCount> Types { get; set; } = [];
    public List<DashboardDailyCount> ScheduledDaily { get; set; } = [];
    public DashboardAgingData OverdueAging { get; set; } = new();
}

public sealed class OrderDashboardData
{
    public int TotalOrders { get; set; }
    public int ActiveOrderCount { get; set; }
    public int CompletedInPeriodCount { get; set; }
    public int CancelledInPeriodCount { get; set; }
    public int ReturnedInPeriodCount { get; set; }
    public List<DashboardCodeCount> CurrentStatuses { get; set; } = [];
    public List<DashboardDailyCount> CompletedDaily { get; set; } = [];
    public List<DashboardDailyCount> CancelledDaily { get; set; } = [];
    public List<DashboardDailyCount> ReturnedDaily { get; set; } = [];
    public DashboardAgingData ActiveAging { get; set; } = new();
}

public sealed record PaymentMethodPerformanceData(int? Method, int TotalCount, int PaidCount, int FailedCount);

public sealed class PaymentDashboardData
{
    public int TotalPayments { get; set; }
    public int PendingCount { get; set; }
    public int PaidInPeriodCount { get; set; }
    public List<DashboardCodeCount> CurrentStatuses { get; set; } = [];
    public List<DashboardDailyCount> PaidDaily { get; set; } = [];
    public List<PaymentMethodPerformanceData> MethodPerformance { get; set; } = [];
    public DashboardAgingData PendingAging { get; set; } = new();
}

public sealed class DisputeDashboardData
{
    public int TotalDisputes { get; set; }
    public int UnresolvedDisputeCount { get; set; }
    public int ResolvedInPeriodCount { get; set; }
    public double? AverageResolutionTimeHours { get; set; }
    public List<DashboardCodeCount> CurrentStatuses { get; set; } = [];
    public List<DashboardCodeCount> Categories { get; set; } = [];
    public List<DashboardCodeCount> UnresolvedCategories { get; set; } = [];
    public List<DashboardDailyCount> OpenedDaily { get; set; } = [];
    public List<DashboardDailyCount> ResolvedDaily { get; set; } = [];
    public DashboardAgingData UnresolvedAging { get; set; } = new();
}

public sealed class OperationOverviewData
{
    public int TotalOrders { get; set; }
    public int ActiveOrderCount { get; set; }
    public int UpcomingAppointmentCount { get; set; }
    public int TodayAppointmentCount { get; set; }
    public int TotalPayments { get; set; }
    public int PendingPaymentCount { get; set; }
    public int TotalDisputes { get; set; }
    public int UnresolvedDisputeCount { get; set; }
    public int ResolvedDisputeInPeriodCount { get; set; }
}
