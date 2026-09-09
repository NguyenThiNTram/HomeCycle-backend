using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Dashboard;

public static class UserRegistrationTrendCalculator
{
    public static UserRegistrationTrendResponse Calculate(
        IReadOnlyList<RegistrationDay> registrations, DateOnly today, int days,
        int forecastDays, UserRole? role, DateTime generatedAtUtc)
    {
        if (days is < 7 or > 90) throw new ArgumentOutOfRangeException(nameof(days));
        if (forecastDays is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(forecastDays));

        var start = today.AddDays(-2 * days);
        var currentStart = today.AddDays(-days);
        var counts = registrations.ToDictionary(x => x.Date, x => x.Count);
        var daily = Enumerable.Range(0, 2 * days)
            .Select(offset => start.AddDays(offset))
            .Select(date => new RegistrationDay(date, counts.GetValueOrDefault(date)))
            .ToArray();
        var previous = daily.Where(x => x.Date < currentStart).Sum(x => x.Count);
        var current = daily.Where(x => x.Date >= currentStart).Sum(x => x.Count);
        var average = (decimal)current / days;

        return new UserRegistrationTrendResponse
        {
            GeneratedAtUtc = generatedAtUtc,
            Role = role,
            PreviousPeriodStart = start,
            CurrentPeriodStart = currentStart,
            CurrentPeriodEndExclusive = today,
            PreviousPeriodRegistrations = previous,
            CurrentPeriodRegistrations = current,
            RegistrationChange = current - previous,
            GrowthPercent = previous == 0 ? null : Math.Round((current - previous) * 100m / previous, 2),
            AverageDailyRegistrations = Math.Round(average, 2),
            Direction = current > previous ? "Increasing" : current < previous ? "Decreasing" : "Stable",
            DailyRegistrations = daily,
            Forecast = new RegistrationForecast
            {
                StartDate = today,
                EndDateExclusive = today.AddDays(forecastDays),
                Days = forecastDays,
                TrainingDays = days,
                ObservedRegistrations = current,
                EstimatedRegistrations = Math.Round(average * forecastDays, 2)
            }
        };
    }
}
