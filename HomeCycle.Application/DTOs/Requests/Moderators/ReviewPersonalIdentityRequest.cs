using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Moderators
{
    public class ReviewPersonalIdentityRequest
    {
        public VerifyStatus Decision { get; set; }
        public string? RejectReason { get; set; }
    }
}
