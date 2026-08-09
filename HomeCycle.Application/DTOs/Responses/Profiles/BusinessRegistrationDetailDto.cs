using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Profiles
{
    public class BusinessRegistrationDetailDto
    {
        public Guid BusinessProfileId { get; set; }
        public string BusinessName { get; set; } = null!;
        public string? FullName { get; set; }
        public string? BusinessDescription { get; set; }
        public string TaxCode { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string City { get; set; } = null!;
        public string IdentityNumber { get; set; } = null!;
        public string IdentityName { get; set; }
        public DateOnly IdentityDob { get; set; }
        public string IdentityAddress { get; set; }
        public string? OperatingScope { get; set; }
        public BusinessModel BusinessModel { get; set; } 
        public BusinessProfileStatus Status { get; set; } 
        public string? RejectReason { get; set; }   


        public string BankCode { get; set; } = null!;
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountName { get; set; } = null!;

       
        public List<BusinessRegistrationDocumentDto> Documents { get; set; } = new();

        public BusinessRegistrationServiceAreaDto ServiceAreas { get; set; }
    }

    public class BusinessRegistrationDocumentDto
    {
        public Guid BusinessDocumentId { get; set; }
        public int DocumentType { get; set; } 
        public string DocumentUrl { get; set; } = null!;
    }

    public class BusinessRegistrationServiceAreaDto
    {
        public Guid BusinessServiceAreaId { get; set; }
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string Ward { get; set; } = null!;
    }
}
