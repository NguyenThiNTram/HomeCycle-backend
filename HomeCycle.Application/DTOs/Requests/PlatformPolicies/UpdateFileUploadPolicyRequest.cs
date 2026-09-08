using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.PlatformPolicies
{
    public class UpdateFileUploadPolicyRequest
    {
        public FileUploadContext Context { get; set; }

        public long? MaxFileSizeBytes { get; set; }

        public List<string>? AllowedExtensions { get; set; }
    }
}
