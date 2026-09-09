using System.Text.Json.Serialization;
using HomeCycle.Application.DTOs.Requests.Dashboard;

namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class DashboardPeriod
{
    public DateOnly From { get; init; }
    public DateOnly ToExclusive { get; init; }
    public DateOnly PreviousFrom { get; init; }
    public DateOnly PreviousToExclusive => From;
    public string TimeZone => "Asia/Ho_Chi_Minh";
    public DashboardGroupBy GroupBy { get; init; }
    public bool IsPartialPeriod { get; init; }
    [JsonIgnore] public DateTime FromUtc => ToUtc(From);
    [JsonIgnore] public DateTime EndUtc => ToUtc(ToExclusive);
    [JsonIgnore] public DateTime PreviousFromUtc => ToUtc(PreviousFrom);
    private static DateTime ToUtc(DateOnly date) => new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
}
public sealed record CountTrend(int CurrentPeriodCount, int PreviousPeriodCount, int Change, decimal? GrowthPercent, string Direction);
public sealed record TimeSeriesPoint(DateOnly From, DateOnly ToExclusive, int Count);
public sealed record ValueSeriesPoint(DateOnly From, DateOnly ToExclusive, decimal Amount);
public sealed record DistributionItem(string Key, string Label, int Count, decimal Percentage);
public sealed record DashboardCountMetric(int TotalCount, CountTrend Trend);

// Database projections, never exposed as endpoint responses.
public sealed record DashboardDailyCount(DateTime Date, int Count);
public sealed record DashboardCodeCount(int? Code, int Count);
public sealed class OperationDashboardData
{
    public int TotalCount { get; set; }
    public int PreviousCount { get; set; }
    public List<DashboardDailyCount> Daily { get; set; } = [];
    public List<DashboardCodeCount> Statuses { get; set; } = [];
    public List<DashboardCodeCount> Types { get; set; } = [];
    public List<DashboardCodeCount> Methods { get; set; } = [];
    public List<DashboardCodeCount> CurrentStatuses { get; set; } = [];
    public Dictionary<string, int> Events { get; set; } = [];
}
