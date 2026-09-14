using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Dispute_Category_Target")]
    public class Dispute_Category_Target
    {
        public int DisputeCategoryId { get; set; }
        public int TargetType { get; set; }

        public Dispute_Category Category { get; set; } = null!;
    }
}
