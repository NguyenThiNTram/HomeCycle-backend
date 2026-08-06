using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class AgreementPreviewResponse
    {
        public Guid NegotiationId { get; set; }
        public bool HasAgreement { get; set; }
        public Guid? AgreementId { get; set; }

        public string UserRole { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanConfirm { get; set; }
    }
}
