using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class RespondDisputeRequest
    {
        public DisputeResponseType ResponseType { get; set; }

        public string? Content { get; set; }

        public List<IFormFile> EvidenceImages { get; set; } = new();
    }
}
