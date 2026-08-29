using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class AppointmentDetailDto
    {
        public Guid AppointmentId { get; set; }
        public Guid AgreementId { get; set; }

        public AppointmentType? AppointmentType { get; set; }
        public AppointmentStatus? AppointmentStatus { get; set; }

        public DateTime? BuyerCheckAt { get; set; }
        public DateTime? SellerCheckAt { get; set; }

        public DateTime? InteractionDeadlineAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public InspectionAppointmentDetailDto? Inspection { get; set; }
        public CollectionAppointmentDetailDto? Collection { get; set; }

        public AppointmentCancellationDto? Cancellation { get; set; }
        public AppointmentRescheduleInfoDto? Reschedule { get; set; }
        public AppointmentOrderSummaryDto Order { get; set; } = null!;
        public AppointmentActionDto Actions { get; set; } = new();
    }

    public class InspectionAppointmentDetailDto
    {
        public DateTime? InspectionDate { get; set; }
        public string? InspectionAddress { get; set; }
    }

    public class CollectionAppointmentDetailDto
    {
        public DateTime? CollectionDate { get; set; }
        public string? PickupAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public DeliveryMethod? DeliveryMethod { get; set; }
    }

    public class AppointmentCancellationDto
    {
        public DateTime CancelledAt { get; set; }
        public string? Reason { get; set; }
    }

    public class AppointmentRescheduleInfoDto
    {
        public Guid OriginalAppointmentId { get; set; }
        public Guid ProposalAppointmentId { get; set; }

        public Guid? RequestedByUserId { get; set; }
        public DateTime? RequestedAt { get; set; }

        public DateTime? ProposedAt { get; set; }

        public bool IsCurrentUserRequester { get; set; }
    }

    public class AppointmentOrderSummaryDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public OrderStatus? OrderStatus { get; set; }
    }

    public class AppointmentActionDto
    {
        public bool CanCheckIn { get; set; }
        public bool CanRequestReschedule { get; set; }
        public bool CanAcceptReschedule { get; set; }
        public bool CanRejectReschedule { get; set; }
        public bool CanCancel { get; set; }
    }

}
