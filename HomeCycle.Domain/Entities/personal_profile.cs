using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class personal_profile
{
    public Guid PersonalProfileId { get; set; }
    public Guid UserId { get; set; }

    public string? FullName { get; set; }

    public string? RepresentativeCode { get; set; }
    public string? RepresentativeName { get; set; }
    public DateOnly? RepresentativeDob { get; set; }
    public string? RepresentativeAddress { get; set; }

    public string? FrontIDCardImage { get; set; }
    public string? BackIDCardImage { get; set; }

    public int? VerificationStatus { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public int ReputationScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public personal_profile()
    {
    }

    public personal_profile(Guid PersonalProfileId, Guid UserId)
    {
        this.PersonalProfileId = PersonalProfileId;
        this.UserId = UserId;
    }
}
