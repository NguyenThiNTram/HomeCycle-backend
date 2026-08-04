using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Entities
{
    public class brand
    {
        public Guid BrandId { get; set; }
        public string BrandName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public brand()
        {
        }

        public brand(Guid BrandId)
        {
            this.BrandId = BrandId;
        }
    }
}
