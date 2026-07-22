using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Profiles
{
    public class BusinessProfileDetailDto
    {
        public UserSummaryDto UserInfo { get; set; } = null!;
        public Guid BusinessProfileId { get; set; }
        public string? BusinessName { get; set; }
        public string? FullName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? TaxCode { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public string IdentityNumber { get; set; } = null!;
        public string? OperatingScope { get; set; }
        public int BusinessModel { get; set; }
        public int Status { get; set; }
        public int ReputationScore { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<BusinessDocumentDto> Documents { get; set; } = new();
        public List<BankAccountSummaryDto> Banks { get; set; } = new();
    }

    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }
    }

    public class BusinessDocumentDto
    {
        public Guid BusinessDocumentId { get; set; }
        public int DocumentType { get; set; }
        public string DocumentUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class BankAccountSummaryDto
    {
        public Guid UserBankId { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public int? VerifyStatus { get; set; }
    }
}
