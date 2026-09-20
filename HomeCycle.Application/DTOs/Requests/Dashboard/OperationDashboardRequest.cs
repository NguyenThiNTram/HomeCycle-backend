using System.ComponentModel.DataAnnotations;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.Dashboard;

public sealed class PaymentDashboardRequest : DashboardPeriodRequest
{
    [EnumDataType(typeof(PaymentStatus))] public PaymentStatus? PaymentStatus { get; set; }
    [EnumDataType(typeof(PaymentMethod))] public PaymentMethod? PaymentMethod { get; set; }
    [EnumDataType(typeof(PaymentType))] public PaymentType? PaymentType { get; set; }
}
public sealed class OrderDashboardRequest : DashboardPeriodRequest
{
    [EnumDataType(typeof(DeliveryMethod))] public DeliveryMethod? DeliveryMethod { get; set; }
    [EnumDataType(typeof(OrderStatus))] public OrderStatus? OrderStatus { get; set; }
}

public sealed class ReportedListingRequest : HomeCycle.Application.Commons.Paginations.PaginationRequest
{
    public bool OpenOnly { get; set; } = true;
    [StringLength(200)] public string? Keyword { get; set; }
}
public sealed class AppointmentDashboardRequest : DashboardPeriodRequest
{
    [EnumDataType(typeof(AppointmentStatus))] public AppointmentStatus? AppointmentStatus { get; set; }
    [EnumDataType(typeof(AppointmentType))] public AppointmentType? AppointmentType { get; set; }
}
public sealed class DisputeDashboardRequest : DashboardPeriodRequest
{
    [EnumDataType(typeof(DisputeStatus))]
    public DisputeStatus? Status { get; set; }

    [EnumDataType(typeof(DisputeTargetType))]
    public DisputeTargetType? TargetType { get; set; }

    [Range(1, int.MaxValue)]
    public int? DisputeCategoryId { get; set; }
}
