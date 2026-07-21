using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class CreateAttributeOptionRequest
    {
        public string OptionValue { get; set; } = null!;
        public int? DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UpdateAttributeOptionRequest
    {
        public Guid? OptionId { get; set; } // Nếu Null => Thêm mới tùy chọn
        public string OptionValue { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
    }
}
