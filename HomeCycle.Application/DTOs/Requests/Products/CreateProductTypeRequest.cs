using HomeCycle.Application.Commons.Paginations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class CreateProductTypeRequest
    {
        public Guid CategoryId { get; set; }
        public string ProductTypeName { get; set; } = null!;
        public string? Description { get; set; }
        public List<CreateAttributeRequest> Attributes { get; set; } = new();
    }

    public class UpdateProductTypeRequest
    {
        public string ProductTypeName { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public List<UpdateAttributeRequest> Attributes { get; set; } = new();
    }

    public class ProductTypeSearchRequest : PaginationRequest
    {
        public Guid? CategoryId { get; set; }
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}
