namespace HomeCycle.Application.SupplierMatching.Models;

public sealed record SupplierMatchQuotaStatus(
    int Limit,
    int Remaining,
    DateTimeOffset ResetsAt);

public sealed record SupplierMatchQuotaReservation(
    bool Reserved,
    int Limit,
    int Remaining,
    DateTimeOffset ResetsAt);
