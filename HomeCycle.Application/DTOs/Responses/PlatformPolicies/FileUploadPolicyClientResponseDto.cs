using HomeCycle.Application.DTOs.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.PlatformPolicies
{
    public class FileUploadPolicyClientResponseDto
    {
        public int Version { get; set; }

        public DateTime UpdatedAt { get; set; }

        public FileUploadPolicyConfigDto Config { get; set; } = new();
    }
}
