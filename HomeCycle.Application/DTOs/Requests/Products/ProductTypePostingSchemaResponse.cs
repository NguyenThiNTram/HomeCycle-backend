using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class ProductTypePostingSchemaResponse
    {
        public Guid ProductTypeId { get; set; }

        public string ProductTypeName { get; set; } = null!;

        public IReadOnlyList<ProductAttributeSchemaResponse> Attributes { get; set; } = [];
    }
    public sealed class ProductAttributeSchemaResponse
    {
        public Guid AttributeId { get; set; }

        public string AttributeName { get; set; } = null!;

        public DataType DataType { get; set; }

        public InputMode InputMode { get; set; }

        public string? Unit { get; set; }

        public bool IsRequired { get; set; }

        public bool IsFilterable { get; set; }

        public IReadOnlyList<AttributeOptionItem> Options { get; set; } = [];
    }
}
