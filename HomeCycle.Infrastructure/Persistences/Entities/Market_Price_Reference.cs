using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Market_Price_Reference")]
public class Market_Price_Reference
{
    [Key] 
    public Guid Id { get; set; }

    public Guid ProductTypeId { get; set; }

    public Guid? BrandId { get; set; }

    [MaxLength(100)] 
    public string ModelNumber { get; set; } = "";

    [MaxLength(100)] 
    public string NormalizedModelNumber { get; set; } = "";

    [MaxLength(255)] 
    public string? ProductName { get; set; }

    [Column(TypeName = "jsonb")] 
    public string KeySpecifications { get; set; } = "{}";

    public bool HasVariants { get; set; }

    [Precision(18, 2)] 
    public decimal PriceVndPerUnit { get; set; }

    [MaxLength(200)] 
    public string SourceName { get; set; } = "";

    [MaxLength(1000)] 
    public string SourceUrl { get; set; } = "";

    public DateTime ObservedAt { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
