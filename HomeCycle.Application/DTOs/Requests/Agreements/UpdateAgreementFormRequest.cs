using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Agreements
{
    public class UpdateAgreementFormRequest
    {
        public AgreementType AgreementType { get; set; }
        public PaymentType PaymentType { get; set; }
        public AgreementDetailsDto AgreementDetails { get; set; } = new();
    }
}
