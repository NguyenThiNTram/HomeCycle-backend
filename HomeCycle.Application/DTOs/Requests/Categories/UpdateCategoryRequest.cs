using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Categories
{
    public class UpdateCategoryRequest
    {
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
