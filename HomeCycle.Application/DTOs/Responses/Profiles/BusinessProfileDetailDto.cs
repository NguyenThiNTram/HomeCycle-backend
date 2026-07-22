using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Profiles
{
    public class BusinessProfileDetailDto
    {
        // 1. Thông tin Tài khoản User
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        // 2. Thông tin Hồ sơ Doanh nghiệp (business_profile)
        public Guid BusinessProfileId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? BusinessDescription { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty;
        public string? OperatingScope { get; set; }
        public int BusinessModel { get; set; }
        public int Status { get; set; }
        public int ReputationScore { get; set; }

        // 3. Thông tin Liên kết (Tài khoản ngân hàng, Giấy tờ, Khu vực hoạt động)
        public BankAccountDto? BankAccount { get; set; }
        public List<BusinessDocumentResponseDto> Documents { get; set; } = new();
        public List<BusinessServiceAreaResponseDto> ServiceAreas { get; set; } = new();
    }

    public class BankAccountDto
    {
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int VerifyStatus { get; set; }
    }

    public class BusinessDocumentResponseDto
    {
        public Guid BusinessDocumentId { get; set; }
        public int DocumentType { get; set; }
        public string DocumentUrl { get; set; } = string.Empty;
    }

    public class BusinessServiceAreaResponseDto
    {
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
    }
}
