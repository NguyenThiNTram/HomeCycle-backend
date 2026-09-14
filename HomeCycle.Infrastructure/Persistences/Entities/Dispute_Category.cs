using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Dispute_Category")]
    [Index(nameof(Code), Name = "uq_dispute_category_code", IsUnique = true)]
    public class Dispute_Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DisputeCategoryId { get; set; }

        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        public ICollection<Dispute_Category_Target> Targets { get; set; } = new List<Dispute_Category_Target>();
        public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
    }
}
