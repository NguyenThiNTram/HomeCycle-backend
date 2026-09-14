using System.ComponentModel.DataAnnotations;

namespace HomeCycle.Application.DTOs.Requests.Dashboard;

public enum DashboardGroupBy { Day, Week, Month }

public class DashboardPeriodRequest : IValidatableObject
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [EnumDataType(typeof(DashboardGroupBy))]
    public DashboardGroupBy GroupBy { get; set; } = DashboardGroupBy.Day;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue != To.HasValue)
            yield return new("Phải truyền cả From và To.", [nameof(From), nameof(To)]);
        if (From is not { } from || To is not { } to) yield break;
        if (from >= to || to.DayNumber - from.DayNumber > 366)
            yield return new("Khoảng thời gian phải từ 1 đến 366 ngày, To không được tính vào kỳ.", [nameof(From), nameof(To)]);
        var clock = validationContext.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        if (from > today || to > today.AddDays(1))
            yield return new("Khoảng thống kê không được vượt quá ngày hôm nay.", [nameof(From), nameof(To)]);
    }
}
