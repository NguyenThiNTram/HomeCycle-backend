using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Audit_Outbox")]
    public partial class Audit_Outbox
    {
        [Key]
        public Guid EventId { get; set; }

        public string Payload { get; set; } = null!;

        public int RetryCount { get; set; }

        public DateTime NextAttemptAtUtc { get; set; }

        public Guid? LeaseId { get; set; }

        public DateTime? LeaseUntilUtc { get; set; }

        public DateTime? FailedAtUtc { get; set; }

        [StringLength(500)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
