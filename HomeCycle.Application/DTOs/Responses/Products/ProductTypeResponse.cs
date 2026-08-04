using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Products
{
    public class ProductTypeResponse
    {
        public Guid ProductTypeId { get; set; }
        public Guid CategoryId { get; set; }
        public string? ProductTypeName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductTypeDetailResponse : ProductTypeResponse
    {
        public IReadOnlyList<ProductAttributeResponse> Attributes { get; set; }
            = new List<ProductAttributeResponse>();
    }
}
