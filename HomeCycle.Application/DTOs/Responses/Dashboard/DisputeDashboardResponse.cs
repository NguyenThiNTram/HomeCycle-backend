namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class DisputeDashboardResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardPeriod Period { get; init; } = new();
    public int TotalCount { get; init; }
    public int PeriodCount { get; init; }
    public IReadOnlyList<DistributionItem> ByCategory { get; init; } = [];
    public IReadOnlyList<DistributionItem> ByStatus { get; init; } = [];
    public IReadOnlyList<string> MostSelectedCategories { get; init; } = [];
    public IReadOnlyList<string> LeastSelectedUsedCategories { get; init; } = [];
    public IReadOnlyList<string> UnselectedCategories { get; init; } = [];
    public int UnknownCategoryCount { get; init; }
}
