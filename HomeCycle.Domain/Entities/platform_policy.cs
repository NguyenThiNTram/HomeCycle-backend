using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class platform_policy
{
    public Guid PolicyId { get; set; }

    public string? PolicyType { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Version { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAt { get; set; }

    public platform_policy()
    {
    }

    public platform_policy(Guid PolicyId)
    {
        this.PolicyId = PolicyId;
    }
}
