using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Auths
{
    public class AuthResponse
    {
        //public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AuthUserDto? User { get; set; }
        public string Email { get; set; } = null!;
        //public string? AccessToken { get; set; }
        //public string? RefreshToken { get; set; }
    }

    public class LoginResponseDto
    {
        public string Message { get; set; } = string.Empty;
        //public AuthUserDto User { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;

        // Metadata giúp Frontend điều hướng theo Role
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Personal", "Business", "Moderator", "Admin"
    }
}
