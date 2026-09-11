using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Appointments
{
    public class ModeratorAppointmentPartyDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class ModeratorAppointmentListItemDto
    {
        public Guid AppointmentId { get; set; }
        public Guid AgreementId { get; set; }
        public AppointmentType? AppointmentType { get; set; }
        public AppointmentStatus? AppointmentStatus { get; set; }

        public Guid? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string? ProductName { get; set; }

        public ModeratorAppointmentPartyDto Buyer { get; set; } = new();
        public ModeratorAppointmentPartyDto Seller { get; set; } = new();

        public DateTime? ScheduledAt { get; set; }
        public string? Location { get; set; }
        public DateTime? BuyerCheckAt { get; set; }
        public DateTime? SellerCheckAt { get; set; }
        public DateTime? LateThresholdAt { get; set; }
        public bool IsOverdue { get; set; }

        public Guid? InspectionFormId { get; set; }
        public InspectionStatus? InspectionStatus { get; set; }
        public InspectionConclusion? InspectionConclusion { get; set; }
        public bool HasInspectionForm => InspectionFormId.HasValue;

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ModeratorInspectionCheckInDto
    {
        public DateTime? BuyerCheckAt { get; set; }
        public DateTime? SellerCheckAt { get; set; }
        public DateTime? CheckInOpenAt { get; set; }
        public bool IsFullyCheckedIn => BuyerCheckAt.HasValue && SellerCheckAt.HasValue;
    }

    public class ModeratorInspectionAppointmentDetailDto
    {
        public DateTime? InspectionDate { get; set; }
        public string? InspectionAddress { get; set; }
        public InspectionFormReferenceDto? InspectionForm { get; set; }
        public ModeratorInspectionCheckInDto CheckIn { get; set; } = new();
    }

    public class ModeratorAppointmentRescheduleDto
    {
        public Guid OriginalAppointmentId { get; set; }
        public Guid ProposalAppointmentId { get; set; }
        public Guid? RequestedByUserId { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ProposedAt { get; set; }
    }

    public class ModeratorAppointmentDetailDto
    {
        public Guid AppointmentId { get; set; }
        public Guid AgreementId { get; set; }
        public AppointmentType? AppointmentType { get; set; }
        public AppointmentStatus? AppointmentStatus { get; set; }

        public ModeratorAppointmentPartyDto Buyer { get; set; } = new();
        public ModeratorAppointmentPartyDto Seller { get; set; } = new();

        public DateTime? LateThresholdAt { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ModeratorInspectionAppointmentDetailDto? Inspection { get; set; }
        public CollectionAppointmentDetailDto? Collection { get; set; }
        public AppointmentCancellationDto? Cancellation { get; set; }
        public ModeratorAppointmentRescheduleDto? Reschedule { get; set; }
        public AppointmentOrderSummaryDto Order { get; set; } = new();
    }

    public sealed class ModeratorAppointmentReadModel
    {
        public Guid AppointmentId { get; init; }
        public Guid AgreementId { get; init; }
        public AppointmentType? AppointmentType { get; init; }
        public AppointmentStatus? AppointmentStatus { get; init; }

        public Guid? OrderId { get; init; }
        public string? OrderCode { get; init; }
        public string? ProductName { get; init; }

        public Guid BuyerId { get; init; }
        public string? BuyerUsername { get; init; }
        public string? BuyerAvatarUrl { get; init; }

        public Guid SellerId { get; init; }
        public string? SellerUsername { get; init; }
        public string? SellerAvatarUrl { get; init; }

        public DateTime? ScheduledAt { get; init; }
        public string? Location { get; init; }
        public DateTime? BuyerCheckAt { get; init; }
        public DateTime? SellerCheckAt { get; init; }
        public DateTime? LateThresholdAt { get; init; }
        public bool IsOverdue { get; init; }

        public Guid? InspectionFormId { get; init; }
        public InspectionStatus? InspectionStatus { get; init; }
        public InspectionConclusion? InspectionConclusion { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
