using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.PlatformPolicies
{
    public class PlatformPolicyResponseDto<TConfig>
    {
        public Guid PolicyId { get; set; }

        public PlatformPolicyType PolicyType { get; set; }

        public string Title { get; set; } = string.Empty;

        public int Version { get; set; }

        public bool IsActive { get; set; }

        public TConfig Config { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
