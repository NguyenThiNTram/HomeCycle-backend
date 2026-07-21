using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Products
{
    public class ProductAttributeOptionResponse
    {
        public Guid OptionId { get; set; }
        public string? OptionValue { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
    }
}
