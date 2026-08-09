using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateIdentityRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty;
        public string IdentityName { get; set; } = string.Empty;
        public DateOnly IdentityDob { get; set; }
        public string IdentityAddress { get; set; } = string.Empty;

        public IFormFile CccdFront { get; set; } = null!;
        public IFormFile CccdBack { get; set; } = null!;
    }
}
