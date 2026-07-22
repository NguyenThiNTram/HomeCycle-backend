using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Moderators
{
    public class ReviewBusinessProfileRequest
    {
        public Guid BusinessProfileId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectReason { get; set; }
    }

}
