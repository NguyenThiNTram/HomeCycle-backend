using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Entities
{
    [Table("Business_Procurement_Preference")]
    public class Business_Procurement_Preference
    {
        [Key]
        public Guid PreferenceId { get; set; }

        [Required]
        public Guid BusinessProfileId { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)[]")]
        public string[] TargetCities { get; set; } = Array.Empty<string>();

        [Required]
        [Column(TypeName = "int[]")]
        public int[] AcceptableDamageLevels { get; set; } = Array.Empty<int>();

        [Required]
        [Column(TypeName = "int[]")]
        public int[] AcceptableFunctionalityStatuses { get; set; } = Array.Empty<int>();

        [Required]
        [Column(TypeName = "int[]")]
        public int[] ProcurementScales { get; set; } = Array.Empty<int>();

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("BusinessProfileId")]
        public virtual Business_Profile BusinessProfile { get; set; } = null!;
    }
}
