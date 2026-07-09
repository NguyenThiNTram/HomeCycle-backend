using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    public class RegisterPersonalRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        //public string ConfirmPassword { get; set; } = null!;

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        
        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }
        public string? FrontIDCardImage { get; set; }
        public string? BackIDCardImage { get; set; }

        //public int? VerificationStatus { get; set; }
        //public Guid? VerifiedBy { get; set; }
        //public DateTime? VerifiedAt { get; set; }
        //public int ReputationScore { get; set; } = 0;

    }
}
