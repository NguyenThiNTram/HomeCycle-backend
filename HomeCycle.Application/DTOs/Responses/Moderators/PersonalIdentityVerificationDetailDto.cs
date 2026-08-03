using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Moderators
{
    public class PersonalIdentityVerificationDetailDto
    {
        public Guid PersonalProfileId { get; set; }
        public Guid UserId { get; set; }

        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }

        public string? FrontIDCardImage { get; set; }
        public string? BackIDCardImage { get; set; }

        public VerifyStatus VerificationStatus { get; set; }
        public string? VerificationRejectReason { get; set; }

        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PendingPersonalVerificationSummaryDto
    {
        public Guid PersonalProfileId { get; set; }
        public string? RepresentativeName { get; set; }
        public VerifyStatus VerificationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
