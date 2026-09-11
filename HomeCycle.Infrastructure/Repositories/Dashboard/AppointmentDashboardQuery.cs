using HomeCycle.Domain.Enums;

namespace HomeCycle.Infrastructure.Repositories.Dashboard;

// SQL-friendly read model and predicates shared by the repository and regression tests.
// They classify existing rows only and never mutate appointment state.
internal sealed class AppointmentDashboardRow
{
    public Guid AppointmentId { get; init; }
    public int? Type { get; init; }
    public int? Status { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public DateTime? LateThresholdAt { get; init; }
    public DateTime? BuyerCheckAt { get; init; }
    public DateTime? SellerCheckAt { get; init; }
    public Guid? RescheduledFromAppointmentId { get; init; }
    public DateTime? SourceRescheduledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}

internal static class AppointmentDashboardQuery
{
    internal const string RescheduledCancellationReason = "Rescheduled";

    internal static IQueryable<AppointmentDashboardRow> Effective(
        IQueryable<AppointmentDashboardRow> rows)
        => rows.Where(row =>
            row.Status != (int)AppointmentStatus.Proposed
            && row.CancellationReason != RescheduledCancellationReason
            && (
                row.RescheduledFromAppointmentId == null
                || (
                    row.SourceRescheduledAt != null
                    && (
                        row.Status != (int)AppointmentStatus.Cancelled
                        || row.CancelledAt > row.SourceRescheduledAt
                    )
                )
            ));

    internal static IQueryable<AppointmentDashboardRow> RescheduleProposals(
        IQueryable<AppointmentDashboardRow> rows)
        => rows.Where(row =>
            row.RescheduledFromAppointmentId != null
            && row.Status == (int)AppointmentStatus.Proposed);

    internal static IQueryable<AppointmentDashboardRow> Overdue(
        IQueryable<AppointmentDashboardRow> effectiveRows,
        DateTime nowUtc)
        => effectiveRows.Where(row =>
            row.LateThresholdAt <= nowUtc
            && (row.Status == (int)AppointmentStatus.Scheduled
                || row.Status == (int)AppointmentStatus.InProgress)
            && (row.Type == (int)AppointmentType.Collection
                || row.BuyerCheckAt == null
                || row.SellerCheckAt == null));

    // Eligible means the effective Inspection has had a real opportunity to check in:
    // its schedule has arrived, or the appointment already started/completed. An
    // appointment cancelled before its schedule is excluded from the denominator.
    internal static IQueryable<AppointmentDashboardRow> EligibleInspections(
        IQueryable<AppointmentDashboardRow> effectiveRows,
        DateTime nowUtc)
        => effectiveRows.Where(row =>
            row.Type == (int)AppointmentType.Inspection
            && row.ScheduledAt != null
            && (row.ScheduledAt <= nowUtc
                || row.Status == (int)AppointmentStatus.InProgress
                || row.Status == (int)AppointmentStatus.Completed)
            && (row.Status != (int)AppointmentStatus.Cancelled
                || row.CancelledAt == null
                || row.CancelledAt >= row.ScheduledAt));

    internal static IQueryable<AppointmentDashboardRow> SuccessfulInPeriod(
        IQueryable<AppointmentDashboardRow> effectiveRows,
        DateTime fromUtc,
        DateTime toUtc)
        => effectiveRows.Where(row =>
            row.Status == (int)AppointmentStatus.Completed
            && row.CompletedAt >= fromUtc
            && row.CompletedAt < toUtc);

    internal static IQueryable<AppointmentDashboardRow> FailedInPeriod(
        IQueryable<AppointmentDashboardRow> effectiveRows,
        DateTime fromUtc,
        DateTime toUtc)
        => effectiveRows.Where(row =>
            (row.Status == (int)AppointmentStatus.Cancelled
             && row.CancelledAt >= fromUtc
             && row.CancelledAt < toUtc)
            || (row.Status == (int)AppointmentStatus.Expired
                && row.ScheduledAt >= fromUtc
                && row.ScheduledAt < toUtc));
}
