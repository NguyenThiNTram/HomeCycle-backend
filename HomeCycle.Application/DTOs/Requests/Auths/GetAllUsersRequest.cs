using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    public sealed class GetAllUsersRequest
    {
        public UserRole? Role { get; set; }

        public UserStatus? Status { get; set; }

        public string? Keyword { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
