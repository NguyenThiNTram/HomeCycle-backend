using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Dispute_Response")]
    public class Dispute_Response
    {
        [Key]
        public Guid DisputeResponseId { get; set; }

        public Guid DisputeId { get; set; }
        public Guid ResponderId { get; set; }
        public int ResponseType { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(DisputeId))]
        public virtual Dispute Dispute { get; set; } = null!;

        [ForeignKey(nameof(ResponderId))]
        public virtual User Responder { get; set; } = null!;
    }
}
