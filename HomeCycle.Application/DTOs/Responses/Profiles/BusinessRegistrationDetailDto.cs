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
        public string? OperatingScope { get; set; }
        public int BusinessModel { get; set; } 
        public int Status { get; set; } 

     
        public string BankCode { get; set; } = null!;
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountName { get; set; } = null!;

       
        public List<BusinessRegistrationDocumentDto> Documents { get; set; } = new();

        public List<BusinessRegistrationServiceAreaDto> ServiceAreas { get; set; } = new();
    }

    public class BusinessRegistrationDocumentDto
    {
        public Guid BusinessDocumentId { get; set; }
        public int DocumentType { get; set; } 
        public string DocumentUrl { get; set; } = null!;
        public int Status { get; set; } 
        public string? RejectReason { get; set; } 
    }

    public class BusinessRegistrationServiceAreaDto
    {
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
    }
}
