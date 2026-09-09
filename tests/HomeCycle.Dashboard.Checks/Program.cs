using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HomeCycle.API.Controllers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Repositories.Dashboard;
using HomeCycle.Application.Services.Dashboard;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

// Dependency-free regression runner: failures throw and produce a non-zero exit code.
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
bool IsValid(object request) => Validator.TryValidateObject(request, new ValidationContext(request), [], true);
var today = new DateOnly(2026, 9, 9);
var now = new DateTime(2026, 9, 9, 1, 0, 0, DateTimeKind.Utc);
UserRegistrationTrendResponse Calculate(params RegistrationDay[] days)
    => UserRegistrationTrendCalculator.Calculate(days, today, 7, 3, null, now);

var empty = Calculate();
Check(empty.DailyRegistrations.Count == 14 && empty.DailyRegistrations.All(x => x.Count == 0), "Fill missing dates including empty periods");
Check(empty.GrowthPercent is null && empty.Forecast.EstimatedRegistrations == 0, "Empty baseline never divides by zero");
Check(empty.Direction == "Stable", "No registrations is stable");

var rising = Calculate(new(today.AddDays(-14), 7), new(today.AddDays(-7), 14), new(today, 999), new(today.AddDays(-15), 999));
Check(rising.PreviousPeriodRegistrations == 7 && rising.CurrentPeriodRegistrations == 14, "Half-open period boundaries exclude today and old data");
Check(rising.GrowthPercent == 100 && rising.RegistrationChange == 7 && rising.Direction == "Increasing", "Positive growth");
Check(rising.AverageDailyRegistrations == 2 && rising.Forecast.EstimatedRegistrations == 6, "Forecast counts zero-registration days in denominator");
Check(rising.Forecast.StartDate == today && rising.Forecast.EndDateExclusive == today.AddDays(3), "Forecast range starts at local today");
var falling = Calculate(new(today.AddDays(-8), 10), new(today.AddDays(-1), 5));
Check(falling.GrowthPercent == -50 && falling.Direction == "Decreasing", "Negative growth");
var first = Calculate(new RegistrationDay(today.AddDays(-1), 1));
Check(first.GrowthPercent is null && first.Direction == "Increasing", "First registrations do not fabricate a percentage");
Check(first.Forecast.EstimatedRegistrations == 0.43m, "Forecast uses unrounded mean");

Check(IsValid(new UserRegistrationTrendRequest()), "Valid defaults");
Check(!IsValid(new UserRegistrationTrendRequest { Days = 0 }), "Reject short training period");
Check(!IsValid(new UserRegistrationTrendRequest { Days = 91 }), "Bound history query");
Check(!IsValid(new UserRegistrationTrendRequest { ForecastDays = 31 }), "Bound forecast");
Check(!IsValid(new UserDashboardScopeRequest { Role = (UserRole)99 }), "Reject undefined roles");
Check(!IsValid(new DashboardUserListRequest { Status = (UserStatus)99 }), "Reject undefined status");
Check(!IsValid(new DashboardUserListRequest { PageNumber = 0 }), "Reject invalid page");
Check(!IsValid(new DashboardUserListRequest { PageSize = 101 }), "Bound page size");
Check(typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Admin", "All dashboard actions require Admin");
Check(!typeof(DashboardController).GetMethods().Any(x => x.IsDefined(typeof(AllowAnonymousAttribute))), "No anonymous dashboard action");

var repository = new FakeRepository();
// 17:00 UTC is midnight on the following day in Vietnam.
var clock = new FixedClock(new DateTimeOffset(2026, 9, 8, 17, 0, 0, TimeSpan.Zero));
var service = new UserDashboardService(repository, clock);
var overview = await service.GetOverviewAsync(null, default);
Check(overview.TotalAccounts == 5 && overview.EmailVerifiedAccounts == 3, "Overview includes deleted and internal accounts");
Check(overview.ByStatus.Count == 4 && overview.ByStatus.Single(x => x.Status == UserStatus.Pending).Count == 0, "Overview returns zero-count statuses");
Check(overview.ByRole.Sum(x => x.Count) == overview.TotalAccounts && overview.ByStatus.Sum(x => x.Count) == overview.TotalAccounts, "Overview totals reconcile");
var trend = await service.GetRegistrationTrendAsync(new() { Days = 7, Role = UserRole.Personal }, default);
Check(repository.Role == UserRole.Personal, "Role filter forwarded to repository");
Check(repository.ToUtc == new DateTime(2026, 9, 8, 17, 0, 0, DateTimeKind.Utc), "UTC+7 midnight upper boundary");
Check(repository.FromUtc == repository.ToUtc.AddDays(-14), "Two equal periods queried");
Check(repository.FromUtc.Kind == DateTimeKind.Utc && trend.CurrentPeriodEndExclusive == today, "UTC database parameters and local response dates");
Console.WriteLine($"PASS: {checks} dashboard regression checks.");

sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

sealed class FakeRepository : IUserDashboardRepository
{
    public UserRole? Role { get; private set; }
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public Task<IReadOnlyList<UserAccountGroup>> GetAccountGroupsAsync(UserRole? role, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<UserAccountGroup>>([
            new(UserRole.Personal, UserStatus.Active, true, 2),
            new(UserRole.Personal, UserStatus.Deleted, false, 2),
            new(UserRole.Admin, UserStatus.Active, true, 1)]);
    public Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct)
        => throw new NotSupportedException();
    public Task<IReadOnlyList<RegistrationDay>> GetRegistrationsAsync(UserRole? role, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        Role = role;
        FromUtc = fromUtc;
        ToUtc = toUtc;
        return Task.FromResult<IReadOnlyList<RegistrationDay>>([]);
    }
}
