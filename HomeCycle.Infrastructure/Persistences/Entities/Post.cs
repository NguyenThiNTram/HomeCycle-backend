using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Post")]
[Index("City", Name = "idx_post_city")]
[Index("OwnerId", Name = "idx_post_owner")]
[Index("Status", Name = "idx_post_status")]
[Index("PostType", Name = "idx_post_type")]
public partial class Post
{
    [Key]
    public Guid PostId { get; set; }

    public Guid OwnerId { get; set; }

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public int RemainingQuantity { get; set; }

    public int? PostType { get; set; }

    [Precision(18, 2)]
    public decimal? BasePrice { get; set; }

    public string? StreetAddress { get; set; }

    [StringLength(100)]
    public string? Ward { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public int? DeliveryMethod { get; set; }

    [StringLength(50)]
    public string? PriorityLevel { get; set; }

    public int? Status { get; set; }

    public bool IsBusinessPosting { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [InverseProperty("Post")]
    public virtual ICollection<Agreement_Form> Agreement_Forms { get; set; } = new List<Agreement_Form>();

    [InverseProperty("Post")]
    public virtual ICollection<Negotiation> Negotiations { get; set; } = new List<Negotiation>();

    [InverseProperty("Post")]
    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    [InverseProperty("Post")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Post")]
    public virtual Product? Product { get; set; }
}
