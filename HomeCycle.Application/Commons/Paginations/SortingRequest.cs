using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Paginations
{
    public class SortingRequest
    {
        public string? SortBy { get; set; }
        public bool Descending { get; set; } = false;
    }
}
