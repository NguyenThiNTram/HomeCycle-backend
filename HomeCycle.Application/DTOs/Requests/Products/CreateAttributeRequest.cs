using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class CreateAttributeRequest
    {
        public string AttributeName { get; set; } = null!;
        public int DataType { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsRequired { get; set; }
        public List<CreateAttributeOptionRequest> Options { get; set; } = new();
    }

    public class UpdateAttributeRequest
    {
        public Guid? AttributeId { get; set; } // Nếu Null => Thêm mới thuộc tính
        public string AttributeName { get; set; } = null!;
        public int DataType { get; set; }
        public string? Unit { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsRequired { get; set; }
        public List<UpdateAttributeOptionRequest> Options { get; set; } = new();
    }
}
