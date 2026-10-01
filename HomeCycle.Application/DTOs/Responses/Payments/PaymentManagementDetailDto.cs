using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Payments
{
    public sealed class PaymentManagementDetailDto
    {
        public PaymentManagementListItemDto Payment { get; set; } = new();
        public List<PaymentTransactionManagementDto> PaymentTransactions { get; set; } = new();
    }
}
