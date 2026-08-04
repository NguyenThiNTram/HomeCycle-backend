using HomeCycle.Application.Commons.Paginations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Brands
{
    public class BrandSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}
