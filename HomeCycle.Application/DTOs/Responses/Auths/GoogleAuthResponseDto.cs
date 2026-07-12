using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Auths
{
    public class GoogleAuthResponseDto
    {
        public bool IsNewUser { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ExternalRegisterToken { get; set; } // Dùng cho user mới
    }
}
