using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("SupplierMatchMonitorState")]
public sealed class SupplierMatchMonitorState
{
    [Key] public Guid BuyPostId { get; set; }
    public Guid UserId { get; set; }
    [Precision(5, 2)] public decimal LastBestScore { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
