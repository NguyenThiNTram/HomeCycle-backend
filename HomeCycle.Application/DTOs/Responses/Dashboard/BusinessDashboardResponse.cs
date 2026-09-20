namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class BusinessOverviewResponse
{
    public DashboardPeriod Period { get; set; } = new();
    public string GrowthBasis => "CurrentStatusOfBusinessAccountsRegisteredInEachPeriod.NotHistoricalStatusSnapshots";
    public IReadOnlyList<BusinessGrowthPoint> GrowthSeries { get; set; } = [];
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalBusinessAccounts { get; set; }
    public int WithProfileCount { get; set; }
    public int WithoutProfileCount => TotalBusinessAccounts - WithProfileCount;
    public int WithSurveyCount { get; set; }
    public int WithoutSurveyCount => WithProfileCount - WithSurveyCount;
    public decimal SurveyCoveragePercent { get; set; }
    public int WithProductTypesCount { get; set; }
    public int WithServiceAreasCount { get; set; }
    public IReadOnlyList<DistributionItem> ByUserStatus { get; set; } = [];
    public IReadOnlyList<DistributionItem> ByProfileStatus { get; set; } = [];
    public IReadOnlyList<DistributionItem> ByBusinessModel { get; set; } = [];
}
public sealed record BusinessDemandItem(string Key, string Label, int BusinessCount, decimal Percentage);
public sealed class BusinessDemandGroup
{
    public int RespondentCount { get; set; }
    public int MissingResponseCount { get; set; }
    public int InvalidResponseBusinessCount { get; set; }
    public IReadOnlyList<BusinessDemandItem> Items { get; set; } = [];
}
public sealed class BusinessDemandResponse
{
    public DateTime GeneratedAtUtc { get; set; }
    public int BusinessCount { get; set; }
    public string DataScope => "CurrentSurvey";
    public BusinessDemandGroup TargetCities { get; set; } = new();
    public BusinessDemandGroup DamageLevels { get; set; } = new();
    public BusinessDemandGroup FunctionalityStatuses { get; set; } = new();
    public BusinessDemandGroup ProcurementScales { get; set; } = new();
    public BusinessDemandGroup ProductTypes { get; set; } = new();
    public BusinessDemandGroup ServiceCities { get; set; } = new();
    public BusinessDemandGroup ServiceWards { get; set; } = new();
}
public sealed record BusinessTradeGroup(string BuyerRole, string SellerRole, int Count, decimal Amount);
public sealed class BusinessPerformanceResponse
{
    public int CreatedBusinessOrderCount { get; init; }
    public decimal? CancellationRate { get; init; }
    public decimal? DisputeRate { get; init; }
    public string RateBasis => "OrdersCreatedInPeriod.WithCurrentBusinessBuyerOrSeller";
    public IReadOnlyList<DistributionItem> CancellationReasons { get; init; } = [];
    public IReadOnlyList<DistributionItem> DisputeReasons { get; init; } = [];
    public IReadOnlyList<BusinessRankingItem> TopSellers { get; init; } = [];
    public IReadOnlyList<BusinessRankingItem> TopBuyers { get; init; } = [];
    public IReadOnlyList<BusinessContributionPoint> ContributionSeries { get; init; } = [];
    public IReadOnlyList<BusinessRegionMetric> TransactionRegions { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int EligiblePaidPaymentCount { get; init; }
    public int BusinessPaymentCount { get; init; }
    public decimal? BusinessPaymentSharePercent { get; init; }
    public int UnclassifiedPaymentCount { get; init; }
    public IReadOnlyList<BusinessTradeGroup> PaymentGroups { get; init; } = [];
    public IReadOnlyList<TimeSeriesPoint> BusinessPaymentSeries { get; init; } = [];
    public int CompletedOrderCount { get; init; }
    public int OrdersWithMissingAmountCount { get; init; }
    public int UnclassifiedOrderCount { get; init; }
    public decimal TotalCompletedOrderValue { get; init; }
    public decimal BusinessPurchaseValue { get; init; }
    public decimal BusinessSalesValue { get; init; }
    public decimal? BusinessSalesSharePercent { get; init; }
    public int BusinessPurchaseOrderCount { get; init; }
    public int BusinessSalesOrderCount { get; init; }
    public int PurchasingBusinessCount { get; init; }
    public int SellingBusinessCount { get; init; }
    public IReadOnlyList<BusinessTradeGroup> CompletedOrderGroups { get; init; } = [];
    public IReadOnlyList<ValueSeriesPoint> BusinessSalesSeries { get; init; } = [];
    public string ValueBasis => "CurrentCompletedOrders.FinalTotalAmount.IncludesConfiguredShipping";
}
// Aggregated read models between repository and service.
public sealed record DashboardTradeDay(DateTime Date, int? BuyerRole, int? SellerRole, int Count, decimal Amount);
public sealed class BusinessPerformanceData
{
    public int CreatedBusinessOrderCount { get; set; }
    public int CancelledBusinessOrderCount { get; set; }
    public int DisputedBusinessOrderCount { get; set; }
    public List<DashboardReasonCount> CancellationReasons { get; set; } = [];
    public List<DashboardReasonCount> DisputeReasons { get; set; } = [];
    public List<BusinessRankingItem> TopSellers { get; set; } = [];
    public List<BusinessRankingItem> TopBuyers { get; set; } = [];
    public List<BusinessRegionMetric> TransactionRegions { get; set; } = [];
    public List<DashboardTradeDay> Payments { get; set; } = [];
    public List<DashboardTradeDay> Orders { get; set; } = [];
    public int OrdersWithMissingAmountCount { get; set; }
    public int PurchasingBusinessCount { get; set; }
    public int SellingBusinessCount { get; set; }
}

public sealed record BusinessGrowthDay(DateTime Date, int Status, int Count);
public sealed record BusinessGrowthPoint(DateOnly From, DateOnly ToExclusive, int RegisteredCount, int CurrentlyActiveCount, int CurrentlySuspendedCount);
public sealed record BusinessRankingItem(Guid UserId, string Name, int CompletedOrderCount, decimal Gmv);
public sealed record BusinessContributionPoint(DateOnly From, DateOnly ToExclusive, int BusinessOrderCount, decimal BusinessGmv, int PersonalOrderCount, decimal PersonalGmv);
public sealed record BusinessRegionMetric(string City, int OrderCount);
public sealed record DashboardReasonCount(string Key, string Label, int Count);
