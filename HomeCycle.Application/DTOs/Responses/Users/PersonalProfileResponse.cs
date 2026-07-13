using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HomeCycle.Application.DTOs.Responses.Users
{
    public class PersonalProfileResponse
    {
        // user
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsEmailVerified { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // personal_profile
        public string? FullName { get; set; }
        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }
        public string? FrontIDCardImage { get; set; }
        public string? BackIDCardImage { get; set; }
        public VerifyStatus? VerificationStatus { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int ReputationScore { get; set; }
        public BankAccountDto? BankAccount { get; set; }
    }

    public class BankAccountDto
    {
        public Guid UserBankId { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
    }
}
