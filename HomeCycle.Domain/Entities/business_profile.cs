using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class business_profile
{
    public Guid BusinessProfileId { get; set; }
    public Guid UserId { get; set; }

    public string? BusinessName { get; set; }
    public string? BusinessDescription { get; set; }
    public string? TaxCode { get; set; }
    public string IdentityNumber { get; set; }
    public string? FullName { get; set; }
    public string? BusinessAddress { get; set; }
    public string? Ward { get; set; }
    public string? City { get; set; }
    public string? OperatingScope { get; set; }
    public int BusinessModel { get; set; }
    public int Status { get; set; }
    public Guid? CurrentModeratorId { get; set; }
    public int ReputationScore { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public business_profile()
    {
    }

    public business_profile(Guid BusinessProfileId, Guid UserId)
    {
        this.BusinessProfileId = BusinessProfileId;
        this.UserId = UserId;
    }
}
