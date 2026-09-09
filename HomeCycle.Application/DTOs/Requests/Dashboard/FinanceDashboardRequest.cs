using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Dashboard
{
    public enum FinanceFlowScope
    {
        Unclassified = 0,
        ExternalIn = 1,
        ExternalOut = 2,
        Internal = 3
    }

    public sealed class FinanceTransactionRequest : DashboardPeriodRequest
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [EnumDataType(typeof(TransactionType))]
        public TransactionType? TransactionType { get; set; }

        [EnumDataType(typeof(WalletTransactionStatus))]
        public WalletTransactionStatus? Status { get; set; }

        [EnumDataType(typeof(ReferenceType))]
        public ReferenceType? ReferenceType { get; set; }

        [EnumDataType(typeof(FinanceFlowScope))]
        public FinanceFlowScope? FlowScope { get; set; }
    }
}
