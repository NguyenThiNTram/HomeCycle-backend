using System.Text.Json.Serialization;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class DashboardPeriod
{
    public DateOnly From { get; init; }
    public DateOnly ToExclusive { get; init; }
    public string TimeZone => "Asia/Ho_Chi_Minh";
    public DashboardGroupBy GroupBy { get; init; }
    public bool IsPartialPeriod { get; init; }
    [JsonIgnore] public DateTime FromUtc => ToUtc(From);
    [JsonIgnore] public DateTime EndUtc => ToUtc(ToExclusive);
    private static DateTime ToUtc(DateOnly date) => new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
}
public sealed record TimeSeriesPoint(DateOnly From, DateOnly ToExclusive, int Count);
public sealed record ValueSeriesPoint(DateOnly From, DateOnly ToExclusive, decimal Amount);
public sealed record DistributionItem(string Key, string Label, int Count, decimal Percentage);
public sealed record AgingBucket(string Key, string Label, int Count, decimal Percentage);

public sealed class ListingMonitorOverviewResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public string TimeZone => "Asia/Ho_Chi_Minh";
    public string ScopeBasis => "CurrentListings.ExcludesDraft.AppliesSearchAndFilters.NoDateFilter";
    public string ActiveBasis => "StoredStatus.Active.ExpiryIsSeparate";
    public string ReportBasis => "DistinctPostsWithUnresolvedPostDisputes";
    public string GrowthBasis => "CreatedAtOfCurrentlyNonDraftListings.Fixed30CompletedDays.NotNetGrowth";
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
    public int SellCount { get; init; }
    public int BuyCount { get; init; }
    public int UnknownPostTypeCount { get; init; }
    public int CurrentlyReportedListingCount { get; init; }
    public IReadOnlyList<DistributionItem> SellStatusDistribution { get; init; } = [];
    public IReadOnlyList<DistributionItem> BuyStatusDistribution { get; init; } = [];
    public IReadOnlyList<ListingTypeCategoryMetric> Categories { get; init; } = [];
    public DashboardPeriod GrowthPeriod { get; init; } = new();
    public IReadOnlyList<ListingGrowthPoint> GrowthSeries { get; init; } = [];
}

public sealed record ListingTypeCategoryMetric(Guid? CategoryId, string Name, int SellCount, int BuyCount, int UnknownPostTypeCount);
public sealed record ListingGrowthPoint(DateOnly Date, int SellCount, int BuyCount, int UnknownPostTypeCount);
public sealed record ListingTypeStatusCount(int? PostType, int? Status, int Count);
public sealed record ListingTypeDailyCount(DateTime Date, int? PostType, int Count);

public sealed class ListingMonitorOverviewData
{
    public int CurrentlyReportedListingCount { get; init; }
    public IReadOnlyList<ListingTypeStatusCount> Statuses { get; init; } = [];
    public IReadOnlyList<ListingTypeCategoryMetric> Categories { get; init; } = [];
    public IReadOnlyList<ListingTypeDailyCount> Daily { get; init; } = [];
}

public sealed class ListingMonitorItem
{
    public Guid PostId { get; init; }
    public Guid OwnerId { get; init; }
    public string? OwnerName { get; init; }
    public UserRole? OwnerRole { get; init; }
    public PostType? PostType { get; init; }
    public PostStatus? Status { get; init; }
    public string? ProductName { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? ProductTypeName { get; init; }
    public string? BrandName { get; init; }
    public string? Description { get; init; }
    public decimal? BasePrice { get; init; }
    public decimal? PriceFrom { get; init; }
    public decimal? PriceTo { get; init; }
    public int Quantity { get; init; }
    public int RemainingQuantity { get; init; }
    public string? City { get; init; }
    public string? Ward { get; init; }
    public string? StreetAddress { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsExpired { get; init; }
    public int OpenReportCount { get; init; }
    public int TotalReportCount { get; init; }
}

public sealed class ListingMonitorDetailResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public ListingMonitorItem Listing { get; init; } = new();
    public DashboardAccountDetail? Owner { get; init; }
}

public sealed class ListingDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int NewListingCount { get; init; }
    public int CurrentlyReportedListingCount { get; init; }
    public string StatusBasis => "CurrentStatusOfListingsCreatedInPeriod.NoApprovalWorkflow";
    public IReadOnlyList<DistributionItem> StatusDistribution { get; init; } = [];
    public IReadOnlyList<ListingCategoryMetric> TopCategoriesByListings { get; init; } = [];
    public IReadOnlyList<ListingCategoryMetric> TopCategoriesByGmv { get; init; } = [];
    public IReadOnlyList<SupplyDemandPoint> SupplyDemandSeries { get; init; } = [];
}
public sealed record ListingCategoryMetric(Guid? CategoryId, string Name, int? ListingCount, int? CompletedOrderCount, decimal? Gmv);
public sealed record SupplyDemandPoint(DateOnly From, DateOnly ToExclusive, int NewListings, int CompletedOrders);
public sealed class ListingDashboardData
{
    public int CurrentlyReportedListingCount { get; set; }
    public List<DashboardCodeCount> Statuses { get; set; } = [];
    public List<ListingCategoryMetric> TopCategoriesByListings { get; set; } = [];
    public List<ListingCategoryMetric> TopCategoriesByGmv { get; set; } = [];
    public List<DashboardDailyCount> ListingsDaily { get; set; } = [];
    public List<DashboardDailyCount> CompletedDaily { get; set; } = [];
}
public sealed class ReportedListingItem
{
    public Guid LatestReportId { get; init; }
    public Guid PostId { get; init; }
    public string? ProductName { get; init; }
    public Guid OwnerId { get; init; }
    public string? OwnerName { get; init; }
    public PostStatus? Status { get; init; }
    public int ReportCount { get; init; }
    public int ReporterCount { get; init; }
    public DateTime LatestReportedAt { get; init; }
    public IReadOnlyList<ReportReasonItem> Reasons { get; set; } = [];
}
public sealed record ReportReasonItem(int? CategoryId, string Name, int Count);

public sealed class SubscriptionDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public string RevenueBasis => "CompletedSubscriptionFeeTransactionsToPlatformRevenueInPeriod.BusinessPackages";
    public int ActiveBusinessCount { get; init; }
    public decimal Revenue { get; init; }
    public IReadOnlyList<SubscriptionPackageMetric> Packages { get; init; } = [];
    public IReadOnlyList<ValueSeriesPoint> RevenueSeries { get; init; } = [];
}
public sealed record SubscriptionPackageMetric(
    Guid PackageId,
    string Name,
    bool IsActive,
    int ActiveBusinessCount,
    int BuyerCountInPeriod,
    int PaidSubscriptions,
    decimal Revenue);
public sealed class SubscriptionDashboardData
{
    public int ActiveBusinessCount { get; set; }
    public List<SubscriptionPackageMetric> Packages { get; set; } = [];
    public List<DashboardAmountDay> RevenueDaily { get; set; } = [];
}

public sealed class UserActivityResponse
{
    public DateTime GeneratedAtUtc { get; set; }
    public string ActivityBasis => "DistinctPersonalAndBusinessActorsWithSuccessfulAuditEvents.TodayAndRolling30CalendarDays.UTC+7";
    public string Limitation => "Chỉ phản ánh hoạt động có ghi audit; không đo toàn bộ lượt đăng nhập, truy cập hoặc người đang online.";
    public int DailyRecordedActiveUsers { get; set; }
    public int MonthlyRecordedActiveUsers { get; set; }
    public IReadOnlyList<RecordedActivityByRole> ByRole { get; set; } = [];
    public int PendingBusinessVerificationCount { get; set; }
    public int PendingPersonalVerificationCount { get; set; }
}
public sealed record RecordedActivityByRole(UserRole Role, int DailyUsers, int MonthlyUsers);
public sealed class DashboardAccountDetail
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string? PhoneNumber { get; init; }
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public bool IsEmailVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProfileName { get; init; }
    public BusinessProfileStatus? BusinessProfileStatus { get; init; }
    public VerifyStatus? PersonalVerificationStatus { get; init; }
    public string? VerificationRejectReason { get; init; }
    public int? ReputationScore { get; init; }
    public DateTime? VerifiedAt { get; init; }
}
