using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Disputes
{
    public class DisputeUserSummaryDto
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public UserRole Role { get; set; }
    }
}
