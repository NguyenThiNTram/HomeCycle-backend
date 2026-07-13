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

namespace HomeCycle.Application.DTOs.Requests.Users
{
    public class UpdatePersonalProfileRequest
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }
    public class UpdateAvatarRequest
    {
        public string AvatarUrl { get; set; } = null!;
    }

    public class UpdateIdCardRequest
    {
        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }
        public string FrontIDCardImage { get; set; } = null!; // Firebase url
        public string BackIDCardImage { get; set; } = null!;  // Firebase url
    }

    public class UpdateBankAccountRequest
    {
        public string BankCode { get; set; } = null!;
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountName { get; set; } = null!;
    }
}
