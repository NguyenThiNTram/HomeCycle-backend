using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Review")]
[Index("OrderId", Name = "Review_OrderId_key", IsUnique = true)]
[Index("RevieweeId", Name = "idx_review_reviewee")]
[Index("ReviewerId", Name = "idx_review_reviewer")]
public partial class Review
{
    [Key]
    public Guid ReviewId { get; set; }

    public Guid OrderId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid RevieweeId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public int? ReviewStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Review")]
    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    [ForeignKey("OrderId")]
    [InverseProperty("Review")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("RevieweeId")]
    [InverseProperty("ReviewReviewees")]
    public virtual User Reviewee { get; set; } = null!;

    [ForeignKey("ReviewerId")]
    [InverseProperty("ReviewReviewers")]
    public virtual User Reviewer { get; set; } = null!;
}
