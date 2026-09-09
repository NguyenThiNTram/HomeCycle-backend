using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Repositories.Dashboard;
using HomeCycle.Application.Interfaces.Services.Dashboard;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Dashboard;

public sealed class DashboardService(IDashboardRepository repository, TimeProvider clock) : IDashboardService
{
    private DashboardPeriod ResolvePeriod(DashboardPeriodRequest request)
    {
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(request,
            new ClockServices(clock), null);
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request, context, true);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        var from = request.From ?? today.AddDays(-30);
        var to = request.To ?? today;
        return new DashboardPeriod
        {
            From = from, ToExclusive = to, PreviousFrom = from.AddDays(-(to.DayNumber - from.DayNumber)),
            GroupBy = request.GroupBy, IsPartialPeriod = to > today
        };
    }

    private sealed class ClockServices(TimeProvider clock) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(TimeProvider) ? clock : null;
    }

    private static CountTrend Trend(OperationDashboardData data)
    {
        var current = data.Daily.Sum(x => x.Count);
        var previous = data.PreviousCount;
        return new(current, previous, current - previous,
            previous == 0 ? null : Math.Round((current - previous) * 100m / previous, 2),
            current > previous ? "Increasing" : current < previous ? "Decreasing" : "Stable");
    }

    private static decimal Percent(decimal count, decimal total) => total == 0 ? 0 : Math.Round(count * 100m / total, 2);

    private static IReadOnlyList<DistributionItem> Distribution<T>(IEnumerable<DashboardCodeCount> counts) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var rows = counts.ToList();
        var total = rows.Sum(x => x.Count);
        var result = values.Select(value =>
        {
            var count = rows.Where(x => x.Code == Convert.ToInt32(value)).Sum(x => x.Count);
            return new DistributionItem(value.ToString(), value.ToString(), count, Percent(count, total));
        }).ToList();
        var known = values.Select(x => Convert.ToInt32(x)).ToHashSet();
        var unknown = rows.Where(x => !x.Code.HasValue || !known.Contains(x.Code.Value)).Sum(x => x.Count);
        if (unknown > 0) result.Add(new("Unspecified", "Unknown / invalid value", unknown, Percent(unknown, total)));
        return result;
    }

    private static IEnumerable<(DateOnly From, DateOnly To)> Buckets(DashboardPeriod period)
    {
        var start = period.From;
        while (start < period.ToExclusive)
        {
            var end = period.GroupBy switch
            {
                DashboardGroupBy.Week => start.AddDays(7 - ((int)start.DayOfWeek + 6) % 7),
                DashboardGroupBy.Month => new DateOnly(start.Year, start.Month, 1).AddMonths(1),
                _ => start.AddDays(1)
            };
            if (end > period.ToExclusive) end = period.ToExclusive;
            yield return (start, end);
            start = end;
        }
    }

    private static IReadOnlyList<TimeSeriesPoint> Series(IEnumerable<DashboardDailyCount> daily, DashboardPeriod period)
    {
        var rows = daily.Select(x => (Date: DateOnly.FromDateTime(x.Date), x.Count)).ToArray();
        return Buckets(period).Select(b => new TimeSeriesPoint(b.From, b.To,
            rows.Where(x => x.Date >= b.From && x.Date < b.To).Sum(x => x.Count))).ToArray();
    }

    public async Task<OperationOverviewResponse> GetOperationOverviewAsync(DashboardPeriodRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        // Queries share a scoped DbContext; execute sequentially.
        var payments = await repository.GetPaymentsAsync(new(), period, ct);
        var orders = await repository.GetOrdersAsync(new(), period, ct);
        var appointments = await repository.GetAppointmentsAsync(new(), period, ct);
        var disputes = await repository.GetDisputesAsync(new(), period, ct);
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period,
            Payments = new(payments.TotalCount, Trend(payments)), Orders = new(orders.TotalCount, Trend(orders)),
            Appointments = new(appointments.TotalCount, Trend(appointments)), Disputes = new(disputes.TotalCount, Trend(disputes)),
            DisputeCurrentStatusCounts = Distribution<DisputeStatus>(disputes.CurrentStatuses)
        };
    }

    public async Task<PaymentDashboardResponse> GetPaymentDashboardAsync(PaymentDashboardRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        var data = await repository.GetPaymentsAsync(request, period, ct);
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period, TotalCount = data.TotalCount,
            CreatedTrend = Trend(data), CreatedSeries = Series(data.Daily, period),
            CreatedInPeriodByCurrentStatus = Distribution<PaymentStatus>(data.Statuses),
            CreatedInPeriodByMethod = Distribution<PaymentMethod>(data.Methods),
            PaidInPeriodCount = data.Events.GetValueOrDefault("Paid")
        };
    }

    public async Task<OrderDashboardResponse> GetOrderDashboardAsync(OrderDashboardRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        var data = await repository.GetOrdersAsync(request, period, ct);
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period, TotalCount = data.TotalCount,
            CreatedTrend = Trend(data), CreatedSeries = Series(data.Daily, period),
            CreatedInPeriodByCurrentStatus = Distribution<OrderStatus>(data.Statuses),
            CompletedInPeriodCount = data.Events.GetValueOrDefault("Completed"),
            CancelledInPeriodCount = data.Events.GetValueOrDefault("Cancelled"),
            ReturnedInPeriodCount = data.Events.GetValueOrDefault("Returned")
        };
    }

    public async Task<AppointmentDashboardResponse> GetAppointmentDashboardAsync(AppointmentDashboardRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        var data = await repository.GetAppointmentsAsync(request, period, ct);
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period, TotalCount = data.TotalCount,
            CreatedTrend = Trend(data), CreatedSeries = Series(data.Daily, period),
            CreatedInPeriodByCurrentStatus = Distribution<AppointmentStatus>(data.Statuses),
            CreatedInPeriodByType = Distribution<AppointmentType>(data.Types),
            RescheduleProposalsCreatedInPeriodCount = data.Events.GetValueOrDefault("Rescheduled"),
            ScheduledDateInPeriodCount = data.Events.GetValueOrDefault("ScheduledDate")
        };
    }

    public async Task<DisputeDashboardResponse> GetDisputeDashboardAsync(DisputeDashboardRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        var data = await repository.GetDisputesAsync(request, period, ct);
        var categories = Distribution<DisputeCategory>(data.Types);
        var used = categories.Where(x => x.Key != "Unspecified" && x.Count > 0).ToArray();
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period,
            TotalCount = data.TotalCount, PeriodCount = data.Daily.Sum(x => x.Count),
            ByCategory = categories, ByStatus = Distribution<DisputeStatus>(data.Statuses),
            MostSelectedCategories = used.Where(x => x.Count == used.Max(y => y.Count)).Select(x => x.Key).ToArray(),
            LeastSelectedUsedCategories = used.Where(x => x.Count == used.Min(y => y.Count)).Select(x => x.Key).ToArray(),
            UnselectedCategories = categories.Where(x => x.Count == 0).Select(x => x.Key).ToArray(),
            UnknownCategoryCount = categories.Where(x => x.Key == "Unspecified").Sum(x => x.Count)
        };
    }

    public async Task<BusinessOverviewResponse> GetBusinessOverviewAsync(BusinessOverviewRequest request, CancellationToken ct)
    {
        var result = await repository.GetBusinessOverviewAsync(request, ct);
        result.GeneratedAtUtc = clock.GetUtcNow().UtcDateTime;
        result.SurveyCoveragePercent = Percent(result.WithSurveyCount, result.WithProfileCount);
        result.ByUserStatus = CompleteDistribution<UserStatus>(result.ByUserStatus);
        result.ByProfileStatus = CompleteDistribution<BusinessProfileStatus>(result.ByProfileStatus);
        result.ByBusinessModel = CompleteDistribution<BusinessModel>(result.ByBusinessModel);
        return result;
    }

    private static IReadOnlyList<DistributionItem> CompleteDistribution<T>(IReadOnlyList<DistributionItem> rows) where T : struct, Enum
        => Distribution<T>(rows.Select(x => new DashboardCodeCount(int.TryParse(x.Key, out var key) ? key : null, x.Count)));

    public async Task<BusinessDemandResponse> GetBusinessDemandAsync(BusinessDemandRequest request, CancellationToken ct)
    {
        var result = await repository.GetBusinessDemandAsync(request, ct);
        result.GeneratedAtUtc = clock.GetUtcNow().UtcDateTime;
        foreach (var group in new[] { result.TargetCities, result.DamageLevels, result.FunctionalityStatuses,
                     result.ProcurementScales, result.ProductTypes, result.ServiceCities, result.ServiceWards })
        {
            group.MissingResponseCount = result.BusinessCount - group.RespondentCount;
            group.Items = group.Items.Select(x => x with { Percentage = Percent(x.BusinessCount, group.RespondentCount) })
                .OrderByDescending(x => x.BusinessCount).ThenBy(x => x.Key).ToArray();
        }
        return result;
    }

    private static bool IsBusiness(int? role) => role == (int)UserRole.Business;
    private static bool IsCustomer(int? role) => role == (int)UserRole.Personal || IsBusiness(role);
    private static string RoleLabel(int? role) => IsCustomer(role) ? ((UserRole)role!.Value).ToString() : "Unknown";

    private static IReadOnlyList<BusinessTradeGroup> TradeGroups(List<DashboardTradeDay> rows)
    {
        var groups = rows.GroupBy(x => new { Buyer = RoleLabel(x.BuyerRole), Seller = RoleLabel(x.SellerRole) })
            .Select(g => new BusinessTradeGroup(g.Key.Buyer, g.Key.Seller, g.Sum(x => x.Count), g.Sum(x => x.Amount))).ToList();
        foreach (var buyer in new[] { "Personal", "Business" })
        foreach (var seller in new[] { "Personal", "Business" })
            if (!groups.Any(x => x.BuyerRole == buyer && x.SellerRole == seller)) groups.Add(new(buyer, seller, 0, 0));
        return groups.OrderBy(x => x.BuyerRole).ThenBy(x => x.SellerRole).ToArray();
    }

    public async Task<BusinessPerformanceResponse> GetBusinessPerformanceAsync(DashboardPeriodRequest request, CancellationToken ct)
    {
        var period = ResolvePeriod(request);
        var data = await repository.GetBusinessPerformanceAsync(period, ct);
        var payments = data.Payments.Sum(x => x.Count);
        var businessPayments = data.Payments.Where(x => IsBusiness(x.BuyerRole) || IsBusiness(x.SellerRole)).ToArray();
        var businessPaymentCount = businessPayments.Sum(x => x.Count);
        var sales = data.Orders.Where(x => IsBusiness(x.SellerRole)).ToArray();
        var purchases = data.Orders.Where(x => IsBusiness(x.BuyerRole)).ToArray();
        var totalValue = data.Orders.Sum(x => x.Amount);
        var salesValue = sales.Sum(x => x.Amount);
        return new()
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime, Period = period,
            EligiblePaidPaymentCount = payments, BusinessPaymentCount = businessPaymentCount,
            BusinessPaymentSharePercent = payments == 0 ? null : Percent(businessPaymentCount, payments),
            UnclassifiedPaymentCount = data.Payments.Where(x => !IsCustomer(x.BuyerRole) || !IsCustomer(x.SellerRole)).Sum(x => x.Count),
            PaymentGroups = TradeGroups(data.Payments),
            BusinessPaymentSeries = Series(businessPayments.Select(x => new DashboardDailyCount(x.Date, x.Count)), period),
            CompletedOrderCount = data.Orders.Sum(x => x.Count), OrdersWithMissingAmountCount = data.OrdersWithMissingAmountCount,
            UnclassifiedOrderCount = data.Orders.Where(x => !IsCustomer(x.BuyerRole) || !IsCustomer(x.SellerRole)).Sum(x => x.Count),
            TotalCompletedOrderValue = totalValue, BusinessPurchaseValue = purchases.Sum(x => x.Amount), BusinessSalesValue = salesValue,
            BusinessSalesSharePercent = totalValue == 0 ? null : Percent(salesValue, totalValue),
            BusinessPurchaseOrderCount = purchases.Sum(x => x.Count), BusinessSalesOrderCount = sales.Sum(x => x.Count),
            PurchasingBusinessCount = data.PurchasingBusinessCount, SellingBusinessCount = data.SellingBusinessCount,
            CompletedOrderGroups = TradeGroups(data.Orders),
            BusinessSalesSeries = Buckets(period).Select(b => new ValueSeriesPoint(b.From, b.To,
                sales.Where(x => DateOnly.FromDateTime(x.Date) >= b.From && DateOnly.FromDateTime(x.Date) < b.To).Sum(x => x.Amount))).ToArray()
        };
    }

    public async Task<UserDashboardOverviewResponse> GetUserOverviewAsync(UserRole? role, CancellationToken ct)
    {
        var groups = await repository.GetAccountGroupsAsync(role, ct);
        return new UserDashboardOverviewResponse
        {
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime,
            Role = role,
            TotalAccounts = groups.Sum(x => x.Count),
            EmailVerifiedAccounts = groups.Where(x => x.IsEmailVerified).Sum(x => x.Count),
            ByStatus = Enum.GetValues<UserStatus>()
                .Select(status => new UserStatusCount(status, groups.Where(x => x.Status == status).Sum(x => x.Count))).ToArray(),
            ByRole = Enum.GetValues<UserRole>()
                .Select(value => new UserRoleCount(value, groups.Where(x => x.Role == value).Sum(x => x.Count))).ToArray()
        };
    }

    public Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct)
        => repository.GetUsersAsync(request, ct);

    public async Task<UserRegistrationTrendResponse> GetUserRegistrationTrendAsync(UserRegistrationTrendRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var endUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime;
        var registrations = await repository.GetRegistrationsAsync(request.Role, endUtc.AddDays(-2 * request.Days), endUtc, ct);
        return UserRegistrationTrendCalculator.Calculate(registrations, today, request.Days, request.ForecastDays, request.Role, now.UtcDateTime);
    }
}
