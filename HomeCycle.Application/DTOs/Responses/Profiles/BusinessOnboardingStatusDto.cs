using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Profiles
{
    public class BusinessOnboardingStatusDto
    {
        public BusinessOnboardingStatus Status { get; set; }
        public bool IsActionRequired { get; set; }
        public string? Message { get; set; }
        public string? RejectReason { get; set; }
        public BusinessOnboardingActionRoute? ActionRoute { get; set; }
    }
}
