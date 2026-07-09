using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class business_service_area
{
    public Guid BusinessServiceAreaId { get; set; }
    public Guid BusinessProfileId { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }

    public int? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public business_service_area()
    {
    }

    public business_service_area(Guid BusinessServiceAreaId, Guid BusinessProfileId)
    {
        this.BusinessServiceAreaId = BusinessServiceAreaId;
        this.BusinessProfileId = BusinessProfileId;
    }
}
