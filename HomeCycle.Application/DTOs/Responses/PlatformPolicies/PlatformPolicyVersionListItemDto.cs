using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.PlatformPolicies
{
    public class PlatformPolicyVersionListItemDto
    {
        public Guid PolicyId { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public bool CanRestore { get; set; }
    }
}
