using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class DisputeSearchRequest : PaginationRequest
    {
        public DisputeStatus? Status { get; set; }

        public DisputeCategory? Category { get; set; }

        public DisputeTargetType? TargetType { get; set; }

        public string? Keyword { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
