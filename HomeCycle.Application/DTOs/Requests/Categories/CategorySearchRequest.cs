using HomeCycle.Application.Commons.Paginations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Categories
{
    public class CategorySearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}
