using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class post
{
    public Guid PostId { get; set; }
    public Guid OwnerId { get; set; }

    public string? Description { get; set; }
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int? PostType { get; set; }
    public decimal? BasePrice { get; set; }
    public string? StreetAddress { get; set; }
    public string? Ward { get; set; }
    public string? City { get; set; }

    public int? DeliveryMethod { get; set; }
    public string? PriorityLevel { get; set; }

    public int? Status { get; set; }

    public bool IsBusinessPosting { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public post()
    {
    }

    public post(Guid PostId, Guid OwnerId)
    {
        this.PostId = PostId;
        this.OwnerId = OwnerId;
    }

}
