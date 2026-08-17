using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class CreateDisputeRequest
    {
       
        public DisputeTargetType TargetType { get; set; }

       
        public Guid TargetId { get; set; }

        public DisputeCategory Category { get; set; }

        public string Description { get; set; } = string.Empty;

        public List<IFormFile> EvidenceImages { get; set; } = new();
    }
}
