using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class review
{
    public Guid ReviewId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid RevieweeId { get; set; }

    public int? Rating { get; set; }
    public string? Comment { get; set; }

    public int? ReviewStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public review()
    {
    }

    public review(Guid ReviewId, Guid OrderId, Guid ReviewerId, Guid RevieweeId)
    {
        this.ReviewId = ReviewId;
        this.OrderId = OrderId;
        this.ReviewerId = ReviewerId;
        this.RevieweeId = RevieweeId;
    }
}
