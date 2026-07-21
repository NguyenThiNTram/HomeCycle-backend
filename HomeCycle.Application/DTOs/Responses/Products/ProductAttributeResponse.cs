using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Products
{
    public class ProductAttributeResponse
    {
        public Guid AttributeId { get; set; }
        public string? AttributeName { get; set; }
        public int? DataType { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsRequired { get; set; }

        public IReadOnlyList<ProductAttributeOptionResponse> Options { get; set; } = new List<ProductAttributeOptionResponse>();
    }
}
