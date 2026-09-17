using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Infrastructure;

[Table("SupplierMatchDailyUsage")]
public sealed class SupplierMatchDailyUsage
{
    public Guid UserId { get; set; }
    public DateOnly UsageDate { get; set; }
    public int RefreshCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
