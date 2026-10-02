namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class AdminDashboardOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public DashboardPeriod ComparisonPeriod { get; init; } = new();
    public string CustomerBasis => "PersonalAndBusinessAccounts.CreatedAt.CurrentRole.IncludesAllStatuses";
    public string GmvBasis => "CurrentCompletedOrders.CompletedAt.FinalTotalAmount.IncludesConfiguredShipping";
    public string RevenueBasis => "CompletedSubscriptionFeeTransactions.CreatedAt.ToPlatformRevenueWallet.AllPackages";
    public string OrderStatusBasis => "CurrentStatusOfOrdersCreatedInPeriod";
    public bool IsComparisonPartial => Period.IsPartialPeriod;
    public AdminDashboardKpis Kpis { get; init; } = new();
    public IReadOnlyList<OrderTradePoint> OrderSeries { get; init; } = [];
    public IReadOnlyList<ValueSeriesPoint> RevenueSeries { get; init; } = [];
    public IReadOnlyList<DistributionItem> CreatedOrderStatusDistribution { get; init; } = [];
    public IReadOnlyList<ListingCategoryMetric> TopCategoriesByGmv { get; init; } = [];
    public AdminDashboardSnapshot Snapshot { get; init; } = new();
    public AdminDashboardDataQuality DataQuality { get; init; } = new();
}

public sealed class AdminDashboardKpis
{
    public DashboardComparisonMetric NewCustomers { get; init; } = new();
    public DashboardComparisonMetric NewListings { get; init; } = new();
    public DashboardComparisonMetric CompletedOrders { get; init; } = new();
    public DashboardComparisonMetric CompletedGmv { get; init; } = new();
    public DashboardComparisonMetric PlatformRevenue { get; init; } = new();
}

public sealed class DashboardComparisonMetric
{
    public decimal Value { get; init; }
    public decimal PreviousValue { get; init; }
    public decimal Change { get; init; }
    public decimal? ChangePercent { get; init; }
    public string Unit { get; init; } = "";
}

public sealed class AdminDashboardSnapshot
{
    public DateTime AsOfUtc { get; init; }
    public DateOnly Today { get; init; }
    public int ActiveOrders { get; init; }
    public int TodayAppointments { get; init; }
    public int UnresolvedDisputes { get; init; }
    public FinanceCountAmountMetric OverdueReleaseOrders { get; init; } = new(0, 0);
    public FinanceCountAmountMetric StalePendingPayments { get; init; } = new(0, 0);
    public int PendingBusinessVerificationCount { get; init; }
    public int PendingPersonalVerificationCount { get; init; }
    public int CurrentlyReportedListingCount { get; init; }
}

public sealed class AdminDashboardDataQuality
{
    public int CompletedOrdersMissingAmountCount { get; init; }
    public int PreviousCompletedOrdersMissingAmountCount { get; init; }
}

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
