namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class BusinessOverviewResponse
{
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
    public List<DashboardTradeDay> Payments { get; set; } = [];
    public List<DashboardTradeDay> Orders { get; set; } = [];
    public int OrdersWithMissingAmountCount { get; set; }
    public int PurchasingBusinessCount { get; set; }
    public int SellingBusinessCount { get; set; }
}
