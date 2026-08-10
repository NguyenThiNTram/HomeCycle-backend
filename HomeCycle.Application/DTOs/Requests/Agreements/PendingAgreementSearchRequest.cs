using HomeCycle.Application.Commons.Paginations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Agreements
{
    public class PendingAgreementSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
    }
}
