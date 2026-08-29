using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.PlatformPolicies
{
    public class PlatformPolicyVersionDetailDto
    {
        public Guid PolicyId { get; set; }
        public PlatformPolicyType PolicyType { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public JsonElement Config { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public bool CanRestore { get; set; }
    }
}
