using System.Text.Json.Serialization;
using HomeCycle.Application.DTOs.Requests.Dashboard;

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
