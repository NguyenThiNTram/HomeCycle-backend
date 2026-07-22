using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateBusinessProfileRequest
    {
        public string? BusinessName { get; set; }
        public string? FullName { get; set; } 
        public string? BusinessDescription { get; set; }
        public string? TaxCode { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public string IdentityNumber { get; set; } = null!;
        public string? OperatingScope { get; set; }
        public int BusinessModel { get; set; }
    }
}
