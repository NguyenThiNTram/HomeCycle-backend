using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class ResolveDisputeRequest
    {
        // Required for Order; content reports are resolved as ViolationConfirmed by the server.
        public DisputeResolutionOutcome? ResolutionOutcome { get; set; }
        public string ModeratorNote { get; set; } = string.Empty;
    }
}
