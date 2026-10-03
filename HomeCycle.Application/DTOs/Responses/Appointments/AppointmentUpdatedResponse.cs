using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public sealed class AppointmentUpdatedResponse
    {
        public Guid AppointmentId { get; init; }
        public Guid AgreementId { get; init; }

        // Thời điểm server đọc snapshot lịch hẹn sau khi commit (UTC).
        // Client bỏ qua event có UpdatedAt cũ hơn dữ liệu đang hiển thị.
        public DateTime UpdatedAt { get; init; }

        // Lịch gốc của lịch đề xuất đổi lịch; giúp client đang xem lịch gốc nhận ra event của lịch đề xuất.
        public Guid? RescheduledFromAppointmentId { get; init; }

        // Trạng thái chung của lịch hẹn, cùng cấu trúc với GET /appointments/{id} nhưng bỏ các field
        // phụ thuộc người xem (Actions, CheckIn.CanCheckIn, Reschedule.IsCurrentUserRequester).
        // Null khi server không đọc được snapshot: client chỉ coi event là tín hiệu tải lại.
        public AppointmentRealtimeSnapshotDto? Appointment { get; init; }
    }

    // Enum gửi dạng chuỗi giống REST (SignalR mặc định gửi enum dạng số).
    public sealed class AppointmentRealtimeSnapshotDto
    {
        public string? AppointmentType { get; init; }
        public string? AppointmentStatus { get; init; }
        public DateTime? LateThresholdAt { get; init; }
        public bool IsOverdue { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public AppointmentRealtimeInspectionDto? Inspection { get; init; }
        public AppointmentRealtimeCollectionDto? Collection { get; init; }
        public AppointmentCancellationDto? Cancellation { get; init; }
        public AppointmentRealtimeRescheduleDto? Reschedule { get; init; }
        public AppointmentRealtimeOrderDto? Order { get; init; }
    }

    public sealed class AppointmentRealtimeInspectionDto
    {
        public DateTime? InspectionDate { get; init; }
        public string? InspectionAddress { get; init; }
        public AppointmentRealtimeInspectionFormDto? InspectionForm { get; init; }
        public AppointmentRealtimeCheckInDto CheckIn { get; init; } = new();
    }

    public sealed class AppointmentRealtimeInspectionFormDto
    {
        public Guid InspectionFormId { get; init; }
        public int Revision { get; init; }
        public string? InspectionStatus { get; init; }
        public string? InspectionMode { get; init; }
        public string? Conclusion { get; init; }
    }

    public sealed class AppointmentRealtimeCheckInDto
    {
        public DateTime? BuyerCheckAt { get; init; }
        public DateTime? SellerCheckAt { get; init; }
        public DateTime? CheckInOpenAt { get; init; }
        public bool IsFullyCheckedIn => BuyerCheckAt.HasValue && SellerCheckAt.HasValue;
    }

    public sealed class AppointmentRealtimeCollectionDto
    {
        public DateTime? CollectionDate { get; init; }
        public string? PickupAddress { get; init; }
        public string? DeliveryAddress { get; init; }
        public string? DeliveryMethod { get; init; }
    }

    public sealed class AppointmentRealtimeRescheduleDto
    {
        public Guid OriginalAppointmentId { get; init; }
        public Guid ProposalAppointmentId { get; init; }
        public Guid? RequestedByUserId { get; init; }
        public DateTime? RequestedAt { get; init; }
        public DateTime? ProposedAt { get; init; }
    }

    public sealed class AppointmentRealtimeOrderDto
    {
        public Guid OrderId { get; init; }
        public string OrderCode { get; init; } = string.Empty;
        public string? ProductName { get; init; }
        public string? OrderStatus { get; init; }
    }
}
