using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Repositories.Dashboard;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure.Repositories.Dashboard;

public sealed class DashboardRepository(HomeCycleDbContext db) : IDashboardRepository
{
    private static readonly int?[] ActiveOrderStatuses =
        [(int)OrderStatus.Pending, (int)OrderStatus.Processing, (int)OrderStatus.Disputing];

    private static readonly int?[] NonTerminalAppointmentStatuses =
        [(int)AppointmentStatus.Proposed, (int)AppointmentStatus.Scheduled, (int)AppointmentStatus.InProgress];

    private static readonly int?[] UnresolvedDisputeStatuses =
        [(int)DisputeStatus.Pending, (int)DisputeStatus.UnderReview, (int)DisputeStatus.AwaitingReturn];

    private sealed class AppointmentScheduleRow
    {
        public DateTime? ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? Status { get; set; }
        public int? Type { get; set; }
        public Guid? RescheduledFromAppointmentId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    private static async Task<DashboardAgingData> GetAgingAsync(
        IQueryable<DateTime> timestamps, DateTime nowUtc, CancellationToken ct)
    {
        var oneDayAgo = nowUtc.AddDays(-1);
        var threeDaysAgo = nowUtc.AddDays(-3);
        var sevenDaysAgo = nowUtc.AddDays(-7);
        var count = await timestamps.CountAsync(ct);
        if (count == 0) return new DashboardAgingData();

        var oldest = await timestamps.MinAsync(ct);
        var averageHours = await timestamps.AverageAsync(timestamp => (nowUtc - timestamp).TotalHours, ct);
        return new DashboardAgingData
        {
            AverageAgeHours = averageHours,
            OldestAgeHours = (nowUtc - oldest).TotalHours,
            UnderOneDayCount = await timestamps.CountAsync(x => x > oneDayAgo, ct),
            OneToThreeDaysCount = await timestamps.CountAsync(x => x <= oneDayAgo && x > threeDaysAgo, ct),
            ThreeToSevenDaysCount = await timestamps.CountAsync(x => x <= threeDaysAgo && x > sevenDaysAgo, ct),
            OverSevenDaysCount = await timestamps.CountAsync(x => x <= sevenDaysAgo, ct)
        };
    }

    private IQueryable<AppointmentScheduleRow> AppointmentScheduleQuery(AppointmentDashboardRequest? request = null)
    {
        var query = db.Appointments.AsNoTracking();
        if (request?.AppointmentStatus.HasValue == true)
            query = query.Where(x => x.AppointmentStatus == (int)request.AppointmentStatus.Value);
        if (request?.AppointmentType.HasValue == true)
            query = query.Where(x => x.AppointmentType == (int)request.AppointmentType.Value);

        return query.Select(x => new AppointmentScheduleRow
        {
            ScheduledAt = x.AppointmentType == (int)AppointmentType.Inspection
                ? x.Inspection_Appointment!.InspectionDate
                : x.AppointmentType == (int)AppointmentType.Collection
                    ? x.Collection_Appointment!.CollectionDate
                    : null,
            CreatedAt = x.CreatedAt,
            Status = x.AppointmentStatus,
            Type = x.AppointmentType,
            RescheduledFromAppointmentId = x.RescheduledFromAppointmentId,
            CompletedAt = x.CompletedAt,
            CancelledAt = x.CancelledAt
        });
    }

    private static (DateTime StartUtc, DateTime EndUtc) CurrentVietnamDay(DateTime nowUtc)
    {
        var today = nowUtc.AddHours(7).Date;
        return (today.AddHours(-7), today.AddDays(1).AddHours(-7));
    }

    public async Task<OperationOverviewData> GetOperationOverviewAsync(
        DashboardPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var (todayStartUtc, todayEndUtc) = CurrentVietnamDay(nowUtc);
        var appointments = AppointmentScheduleQuery();
        var unresolvedDisputes = db.Disputes.AsNoTracking()
            .Where(x => UnresolvedDisputeStatuses.Contains(x.DisputeStatus));

        return new OperationOverviewData
        {
            TotalOrders = await db.Orders.AsNoTracking().CountAsync(ct),
            ActiveOrderCount = await db.Orders.AsNoTracking()
                .CountAsync(x => ActiveOrderStatuses.Contains(x.OrderStatus), ct),
            UpcomingAppointmentCount = await appointments.CountAsync(x =>
                x.Status == (int)AppointmentStatus.Scheduled && x.ScheduledAt >= nowUtc, ct),
            TodayAppointmentCount = await appointments.CountAsync(x =>
                NonTerminalAppointmentStatuses.Contains(x.Status)
                && x.ScheduledAt >= todayStartUtc && x.ScheduledAt < todayEndUtc, ct),
            TotalPayments = await db.Payments.AsNoTracking().CountAsync(ct),
            PendingPaymentCount = await db.Payments.AsNoTracking()
                .CountAsync(x => x.PaymentStatus == (int)PaymentStatus.Pending, ct),
            TotalDisputes = await db.Disputes.AsNoTracking().CountAsync(ct),
            UnresolvedDisputeCount = await unresolvedDisputes.CountAsync(ct),
            ResolvedDisputeInPeriodCount = await db.Disputes.AsNoTracking().CountAsync(x =>
                x.ResolvedAt >= period.FromUtc && x.ResolvedAt < period.EndUtc, ct)
        };
    }

    public async Task<PaymentDashboardData> GetPaymentsAsync(
        PaymentDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var query = db.Payments.AsNoTracking();
        if (request.PaymentStatus.HasValue)
            query = query.Where(x => x.PaymentStatus == (int)request.PaymentStatus.Value);
        if (request.PaymentMethod.HasValue)
            query = query.Where(x => x.PaymentMethod == (int)request.PaymentMethod.Value);
        if (request.PaymentType.HasValue)
            query = query.Where(x => x.PaymentType == (int)request.PaymentType.Value);

        var pending = query.Where(x => x.PaymentStatus == (int)PaymentStatus.Pending);
        var paidInPeriod = query.Where(x => x.PaidAt >= period.FromUtc && x.PaidAt < period.EndUtc);

        return new PaymentDashboardData
        {
            TotalPayments = await query.CountAsync(ct),
            PendingCount = await pending.CountAsync(ct),
            PaidInPeriodCount = await paidInPeriod.CountAsync(ct),
            CurrentStatuses = await query.GroupBy(x => (int?)x.PaymentStatus)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            PaidDaily = await paidInPeriod.GroupBy(x => x.PaidAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            MethodPerformance = await query.GroupBy(x => (int?)x.PaymentMethod)
                .Select(g => new PaymentMethodPerformanceData(
                    g.Key,
                    g.Count(),
                    g.Count(x => x.PaidAt != null),
                    g.Count(x => x.PaymentStatus == (int)PaymentStatus.Failed)))
                .ToListAsync(ct),
            PendingAging = await GetAgingAsync(pending.Select(x => x.CreatedAt), nowUtc, ct)
        };
    }

    public async Task<OrderDashboardData> GetOrdersAsync(
        OrderDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();
        if (request.OrderStatus.HasValue)
            query = query.Where(x => x.OrderStatus == (int)request.OrderStatus.Value);

        var active = query.Where(x => ActiveOrderStatuses.Contains(x.OrderStatus));
        var completed = query.Where(x => x.CompletedAt >= period.FromUtc && x.CompletedAt < period.EndUtc);
        var cancelled = query.Where(x => x.CancelledAt >= period.FromUtc && x.CancelledAt < period.EndUtc);
        var returned = query.Where(x => x.ReturnedAt >= period.FromUtc && x.ReturnedAt < period.EndUtc);

        return new OrderDashboardData
        {
            TotalOrders = await query.CountAsync(ct),
            ActiveOrderCount = await active.CountAsync(ct),
            CompletedInPeriodCount = await completed.CountAsync(ct),
            CancelledInPeriodCount = await cancelled.CountAsync(ct),
            ReturnedInPeriodCount = await returned.CountAsync(ct),
            CurrentStatuses = await query.GroupBy(x => (int?)x.OrderStatus)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            CompletedDaily = await completed.GroupBy(x => x.CompletedAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            CancelledDaily = await cancelled.GroupBy(x => x.CancelledAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            ReturnedDaily = await returned.GroupBy(x => x.ReturnedAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            ActiveAging = await GetAgingAsync(active.Select(x => x.CreatedAt), nowUtc, ct)
        };
    }

    public async Task<AppointmentDashboardData> GetAppointmentsAsync(
        AppointmentDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var query = AppointmentScheduleQuery(request);
        var (todayStartUtc, todayEndUtc) = CurrentVietnamDay(nowUtc);
        var overdue = query.Where(x => x.ScheduledAt < nowUtc
            && NonTerminalAppointmentStatuses.Contains(x.Status));
        var scheduledInPeriod = query.Where(x =>
            x.ScheduledAt >= period.FromUtc && x.ScheduledAt < period.EndUtc);

        return new AppointmentDashboardData
        {
            TotalAppointments = await query.CountAsync(ct),
            UpcomingCount = await query.CountAsync(x =>
                x.Status == (int)AppointmentStatus.Scheduled && x.ScheduledAt >= nowUtc, ct),
            TodayCount = await query.CountAsync(x =>
                NonTerminalAppointmentStatuses.Contains(x.Status)
                && x.ScheduledAt >= todayStartUtc && x.ScheduledAt < todayEndUtc, ct),
            PendingCount = await query.CountAsync(x =>
                x.Status == (int)AppointmentStatus.Proposed, ct),
            CompletedInPeriodCount = await query.CountAsync(x =>
                x.CompletedAt >= period.FromUtc && x.CompletedAt < period.EndUtc, ct),
            CancelledInPeriodCount = await query.CountAsync(x =>
                x.CancelledAt >= period.FromUtc && x.CancelledAt < period.EndUtc, ct),
            ExpiredCount = await query.CountAsync(x =>
                x.Status == (int)AppointmentStatus.Expired, ct),
            RescheduleProposalCount = await query.CountAsync(x =>
                x.RescheduledFromAppointmentId != null, ct),
            OverdueCount = await overdue.CountAsync(ct),
            CurrentStatuses = await query.GroupBy(x => x.Status)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            Types = await query.GroupBy(x => x.Type)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            ScheduledDaily = await scheduledInPeriod.GroupBy(x => x.ScheduledAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            OverdueAging = await GetAgingAsync(overdue.Select(x => x.ScheduledAt!.Value), nowUtc, ct)
        };
    }

    public async Task<DisputeDashboardData> GetDisputesAsync(
        DisputeDashboardRequest request, DashboardPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var query = db.Disputes.AsNoTracking();
        if (request.Status.HasValue)
            query = query.Where(x => x.DisputeStatus == (int)request.Status.Value);
        if (request.Category.HasValue)
            query = query.Where(x => x.DisputeCategory == (int)request.Category.Value);
        if (request.TargetType.HasValue)
            query = query.Where(x => x.DisputeTargetType == (int)request.TargetType.Value);

        var unresolved = query.Where(x => UnresolvedDisputeStatuses.Contains(x.DisputeStatus));
        var opened = query.Where(x => x.CreatedAt >= period.FromUtc && x.CreatedAt < period.EndUtc);
        var resolved = query.Where(x =>
            x.ResolvedAt >= period.FromUtc && x.ResolvedAt < period.EndUtc);

        return new DisputeDashboardData
        {
            TotalDisputes = await query.CountAsync(ct),
            UnresolvedDisputeCount = await unresolved.CountAsync(ct),
            ResolvedInPeriodCount = await resolved.CountAsync(ct),
            AverageResolutionTimeHours = await resolved
                .AverageAsync(x => (double?)(x.ResolvedAt!.Value - x.CreatedAt).TotalHours, ct),
            CurrentStatuses = await query.GroupBy(x => (int?)x.DisputeStatus)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            Categories = await query.GroupBy(x => (int?)x.DisputeCategory)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            UnresolvedCategories = await unresolved.GroupBy(x => (int?)x.DisputeCategory)
                .Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct),
            OpenedDaily = await opened.GroupBy(x => x.CreatedAt.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            ResolvedDaily = await resolved.GroupBy(x => x.ResolvedAt!.Value.AddHours(7).Date)
                .Select(g => new DashboardDailyCount(g.Key, g.Count())).ToListAsync(ct),
            UnresolvedAging = await GetAgingAsync(unresolved.Select(x => x.CreatedAt), nowUtc, ct)
        };
    }

    private IQueryable<User> BusinessAccounts(BusinessOverviewRequest request)
    {
        var query = db.Users.AsNoTracking().Where(x => x.Role == (int)UserRole.Business);
        if (request.UserStatus.HasValue) query = query.Where(x => x.Status == (int)request.UserStatus.Value);
        if (request.ProfileStatus.HasValue)
            query = query.Where(x => x.Business_Profile != null && x.Business_Profile.Status == (int)request.ProfileStatus.Value);
        if (request.BusinessModel.HasValue)
            query = query.Where(x => x.Business_Profile != null && x.Business_Profile.BusinessModel == (int)request.BusinessModel.Value);
        return query;
    }

    public async Task<BusinessOverviewResponse> GetBusinessOverviewAsync(BusinessOverviewRequest request, CancellationToken ct)
    {
        var users = BusinessAccounts(request);
        var profiles = db.Business_Profiles.AsNoTracking().Where(p => users.Any(u => u.UserId == p.UserId));
        var status = await users.GroupBy(x => x.Status).Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct);
        var profileStatus = await profiles.GroupBy(x => x.Status).Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct);
        var model = await profiles.GroupBy(x => x.BusinessModel).Select(g => new DashboardCodeCount(g.Key, g.Count())).ToListAsync(ct);
        return new()
        {
            TotalBusinessAccounts = status.Sum(x => x.Count), WithProfileCount = profileStatus.Sum(x => x.Count),
            WithSurveyCount = await profiles.CountAsync(p => db.Business_Procurement_Preferences.Any(x => x.BusinessProfileId == p.BusinessProfileId), ct),
            WithProductTypesCount = await profiles.CountAsync(p => p.Business_Product_Types.Any(), ct),
            WithServiceAreasCount = await profiles.CountAsync(p => p.Business_Service_Areas.Any(), ct),
            ByUserStatus = status.Select(x => new DistributionItem(x.Code.ToString()!, "", x.Count, 0)).ToArray(),
            ByProfileStatus = profileStatus.Select(x => new DistributionItem(x.Code.ToString()!, "", x.Count, 0)).ToArray(),
            ByBusinessModel = model.Select(x => new DistributionItem(x.Code.ToString()!, "", x.Count, 0)).ToArray()
        };
    }

    private sealed class BusinessAnswer
    {
        public Guid BusinessProfileId { get; set; }
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
    }
    private sealed class BusinessCodeAnswer
    {
        public Guid BusinessProfileId { get; set; }
        public int Code { get; set; }
    }

    private static async Task<BusinessDemandGroup> TextAnswersAsync(IQueryable<BusinessAnswer> answers, CancellationToken ct)
    {
        var valid = answers.Where(x => x.Key != "");
        var respondentCount = await valid.Select(x => x.BusinessProfileId).Distinct().CountAsync(ct);
        var items = await valid.GroupBy(x => new { x.Key, x.Label })
            .Select(g => new BusinessDemandItem(g.Key.Key, g.Key.Label, g.Select(x => x.BusinessProfileId).Distinct().Count(), 0))
            .ToListAsync(ct);
        return new() { RespondentCount = respondentCount, Items = items };
    }

    private static async Task<BusinessDemandGroup> CodeAnswersAsync<T>(IQueryable<BusinessCodeAnswer> answers, CancellationToken ct) where T : struct, Enum
    {
        var codes = Enum.GetValues<T>().Select(x => Convert.ToInt32(x)).ToArray();
        var valid = answers.Where(x => codes.Contains(x.Code));
        var groups = await valid.GroupBy(x => x.Code)
            .Select(g => new DashboardCodeCount(g.Key, g.Select(x => x.BusinessProfileId).Distinct().Count())).ToListAsync(ct);
        return new()
        {
            RespondentCount = await valid.Select(x => x.BusinessProfileId).Distinct().CountAsync(ct),
            InvalidResponseBusinessCount = await answers.Where(x => !codes.Contains(x.Code)).Select(x => x.BusinessProfileId).Distinct().CountAsync(ct),
            Items = Enum.GetValues<T>().Select(value => new BusinessDemandItem(value.ToString(), value.ToString(),
                groups.Where(x => x.Code == Convert.ToInt32(value)).Sum(x => x.Count), 0)).ToArray()
        };
    }

    public async Task<BusinessDemandResponse> GetBusinessDemandAsync(BusinessDemandRequest request, CancellationToken ct)
    {
        var users = BusinessAccounts(request);
        var profiles = db.Business_Profiles.AsNoTracking().Where(p => users.Any(u => u.UserId == p.UserId));
        // Fixed SQL identifiers only. EF composes parameterized filters/aggregates over these read models.
        // The configured Npgsql version cannot translate correlated SelectMany on these array properties.
        var targetCities = db.Database.SqlQuery<BusinessAnswer>($"""
            SELECT p."BusinessProfileId", COALESCE(lower(btrim(c.value)), '') AS "Key",
                   COALESCE(lower(btrim(c.value)), '') AS "Label"
            FROM "Business_Procurement_Preference" AS p
            CROSS JOIN LATERAL unnest(p."TargetCities") AS c(value)
            """);
        var damageLevels = db.Database.SqlQuery<BusinessCodeAnswer>($"""
            SELECT p."BusinessProfileId", c.value AS "Code"
            FROM "Business_Procurement_Preference" AS p
            CROSS JOIN LATERAL unnest(p."AcceptableDamageLevels") AS c(value)
            WHERE c.value IS NOT NULL
            """);
        var functionalities = db.Database.SqlQuery<BusinessCodeAnswer>($"""
            SELECT p."BusinessProfileId", c.value AS "Code"
            FROM "Business_Procurement_Preference" AS p
            CROSS JOIN LATERAL unnest(p."AcceptableFunctionalityStatuses") AS c(value)
            WHERE c.value IS NOT NULL
            """);
        var scales = db.Database.SqlQuery<BusinessCodeAnswer>($"""
            SELECT p."BusinessProfileId", c.value AS "Code"
            FROM "Business_Procurement_Preference" AS p
            CROSS JOIN LATERAL unnest(p."ProcurementScales") AS c(value)
            WHERE c.value IS NOT NULL
            """);
        if (!string.IsNullOrWhiteSpace(request.TargetCity))
        {
            var city = request.TargetCity.Trim().ToLowerInvariant();
            profiles = profiles.Where(p => targetCities.Any(c => c.BusinessProfileId == p.BusinessProfileId && c.Key == city));
        }
        if (!string.IsNullOrWhiteSpace(request.ServiceCity))
        {
            var city = request.ServiceCity.Trim().ToLowerInvariant();
            profiles = profiles.Where(p => p.Business_Service_Areas.Any(a => a.City != null && a.City.Trim().ToLower() == city));
        }
        if (request.ProductTypeId.HasValue)
            profiles = profiles.Where(p => p.Business_Product_Types.Any(t => t.ProductTypeId == request.ProductTypeId.Value));
        var areas = db.Business_Service_Areas.AsNoTracking().Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId));
        var types = db.Business_Product_Types.AsNoTracking().Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId));

        var result = new BusinessDemandResponse { BusinessCount = await profiles.CountAsync(ct) };
        result.TargetCities = await TextAnswersAsync(targetCities.Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId)), ct);
        result.DamageLevels = await CodeAnswersAsync<DamageLevel>(damageLevels.Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId)), ct);
        result.FunctionalityStatuses = await CodeAnswersAsync<FunctionalityStatus>(functionalities.Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId)), ct);
        result.ProcurementScales = await CodeAnswersAsync<ProcurementScale>(scales.Where(x => profiles.Any(p => p.BusinessProfileId == x.BusinessProfileId)), ct);
        result.ProductTypes = await TextAnswersAsync(types.Select(t => new BusinessAnswer
        { BusinessProfileId = t.BusinessProfileId, Key = t.ProductTypeId.ToString(), Label = t.ProductType.ProductTypeName ?? "Unnamed" }), ct);
        result.ServiceCities = await TextAnswersAsync(areas.Select(a => new BusinessAnswer
        { BusinessProfileId = a.BusinessProfileId, Key = a.City == null ? "" : a.City.Trim().ToLower(), Label = a.City == null ? "" : a.City.Trim().ToLower() }), ct);
        result.ServiceWards = await TextAnswersAsync(areas.Where(a => a.City != null && a.City.Trim() != "" && a.Ward != null && a.Ward.Trim() != "")
            .Select(a => new BusinessAnswer
            { BusinessProfileId = a.BusinessProfileId, Key = a.City!.Trim().ToLower() + " / " + a.Ward!.Trim().ToLower(), Label = a.City!.Trim().ToLower() + " / " + a.Ward!.Trim().ToLower() }), ct);
        return result;
    }

    public async Task<BusinessPerformanceData> GetBusinessPerformanceAsync(DashboardPeriod period, CancellationToken ct)
    {
        var from = period.FromUtc;
        var to = period.EndUtc;
        var payments = db.Payments.AsNoTracking().Where(x => x.PaidAt >= from && x.PaidAt < to
            && (x.PaymentType == (int)PaymentType.Deposit || x.PaymentType == (int)PaymentType.Full_Payment)
            && (x.PaymentStatus == (int)PaymentStatus.Completed || x.PaymentStatus == (int)PaymentStatus.Refunded || x.PaymentStatus == (int)PaymentStatus.PartiallyRefunded))
            .Select(x => new
            {
                Date = x.PaidAt!.Value.AddHours(7).Date,
                BuyerRole = x.Agreement != null ? (int?)x.Agreement.Buyer.Role : x.Order != null ? (int?)x.Order.Agreement.Buyer.Role : null,
                SellerRole = x.Agreement != null ? (int?)x.Agreement.Seller.Role : x.Order != null ? (int?)x.Order.Agreement.Seller.Role : null,
                Amount = x.Amount ?? 0
            });
        var orders = db.Orders.AsNoTracking().Where(x => x.OrderStatus == (int)OrderStatus.Completed && x.CompletedAt >= from && x.CompletedAt < to);
        return new()
        {
            Payments = await payments.GroupBy(x => new { x.Date, x.BuyerRole, x.SellerRole })
                .Select(g => new DashboardTradeDay(g.Key.Date, g.Key.BuyerRole, g.Key.SellerRole, g.Count(), g.Sum(x => x.Amount))).ToListAsync(ct),
            Orders = await orders.GroupBy(x => new { Date = x.CompletedAt!.Value.AddHours(7).Date, BuyerRole = (int?)x.Agreement.Buyer.Role, SellerRole = (int?)x.Agreement.Seller.Role })
                .Select(g => new DashboardTradeDay(g.Key.Date, g.Key.BuyerRole, g.Key.SellerRole, g.Count(), g.Sum(x => x.FinalTotalAmount ?? 0))).ToListAsync(ct),
            OrdersWithMissingAmountCount = await orders.CountAsync(x => x.FinalTotalAmount == null, ct),
            PurchasingBusinessCount = await orders.Where(x => x.Agreement.Buyer.Role == (int)UserRole.Business).Select(x => x.Agreement.BuyerId).Distinct().CountAsync(ct),
            SellingBusinessCount = await orders.Where(x => x.Agreement.Seller.Role == (int)UserRole.Business).Select(x => x.Agreement.SellerId).Distinct().CountAsync(ct)
        };
    }

    private IQueryable<User> Scope(UserRole? role)
    {
        var query = db.Users.AsNoTracking();
        return role.HasValue ? query.Where(x => x.Role == (int)role.Value) : query;
    }

    public async Task<IReadOnlyList<UserAccountGroup>> GetAccountGroupsAsync(UserRole? role, CancellationToken ct)
        => await Scope(role)
            .GroupBy(x => new { x.Role, x.Status, x.IsEmailVerified })
            .Select(g => new UserAccountGroup((UserRole)g.Key.Role, (UserStatus)g.Key.Status, g.Key.IsEmailVerified, g.Count()))
            .ToListAsync(ct);

    public async Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct)
    {
        var query = Scope(request.Role);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == (int)request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(x => x.Username.ToLower().Contains(keyword)
                || x.Email.ToLower().Contains(keyword)
                || (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.UserId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new UserAdminResponse
            {
                UserId = x.UserId, Username = x.Username, Email = x.Email,
                PhoneNumber = x.PhoneNumber, AvatarUrl = x.AvatarUrl,
                Role = (UserRole)x.Role, Status = (UserStatus)x.Status,
                IsEmailVerified = x.IsEmailVerified, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
        return new PagedResult<UserAdminResponse>
        {
            Items = items, TotalCount = count, PageNumber = request.PageNumber, PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<RegistrationDay>> GetRegistrationsAsync(UserRole? role, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        // Group in PostgreSQL, not by loading individual user records. Vietnam is UTC+7.
        var rows = await Scope(role).Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .GroupBy(x => x.CreatedAt.AddHours(7).Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date).ToListAsync(ct);
        return rows.Select(x => new RegistrationDay(DateOnly.FromDateTime(x.Date), x.Count)).ToArray();
    }
}
