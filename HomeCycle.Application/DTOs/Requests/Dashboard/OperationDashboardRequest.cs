using System.ComponentModel.DataAnnotations;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.Dashboard;

public enum ListingMonitorSort { Newest, MostOpenReports }

public sealed class ListingMonitorRequest : HomeCycle.Application.Commons.Paginations.PaginationRequest, IValidatableObject
{
    [StringLength(200)] public string? Keyword { get; set; }
    [EnumDataType(typeof(PostType))] public PostType? PostType { get; set; }
    [EnumDataType(typeof(PostStatus))] public PostStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [EnumDataType(typeof(UserRole))] public UserRole? OwnerRole { get; set; }
    public bool? HasOpenReports { get; set; }
    [EnumDataType(typeof(ListingMonitorSort))] public ListingMonitorSort SortBy { get; set; } = ListingMonitorSort.Newest;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageNumber > 1000000)
            yield return new("PageNumber không được vượt quá 1000000.", [nameof(PageNumber)]);
        if (Status == PostStatus.Draft)
            yield return new("Dashboard bài đăng không hỗ trợ trạng thái Nháp.", [nameof(Status)]);
        if (OwnerRole.HasValue && OwnerRole != UserRole.Personal && OwnerRole != UserRole.Business)
            yield return new("Người đăng phải thuộc role Personal hoặc Business.", [nameof(OwnerRole)]);
    }
}

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
