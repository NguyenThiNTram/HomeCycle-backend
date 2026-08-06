using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateBusinessRegistrationRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? BusinessDescription { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? OperatingScope { get; set; }

        public IFormFile BusinessRegistrationCertificate { get; set; }
    }
}
