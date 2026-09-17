using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("SupplierMatchNotificationDedupe")]
public sealed class SupplierMatchNotificationDedupe
{
    public Guid BuyPostId { get; set; }
    public Guid SellPostId { get; set; }
    [Precision(5, 2)] public decimal LastScore { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
}
