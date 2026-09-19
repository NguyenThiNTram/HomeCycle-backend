using HomeCycle.Application.Commons.Paginations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Negotiates
{
    public sealed class ModeratorNegotiationSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
    }
}
