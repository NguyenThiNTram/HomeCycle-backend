using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Infrastructure;

[Table("PriceSuggestionDailyUsage")]
public sealed class PriceSuggestionDailyUsage
{
    public Guid UserId { get; set; }
    public DateOnly UsageDate { get; set; }
    [Range(1, 5)] public int UsageCount { get; set; }
}
