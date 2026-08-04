using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Posts
{
    public class AttributeFilterOptionResponse
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; } = null!;
        public DataType DataType { get; set; }
        public string? Unit { get; set; }

        /// Null nếu Attribute không dùng Option
        public List<AttributeOptionItem> Options { get; set; } = new();
    }

    public class AttributeOptionItem
    {
        public Guid OptionId { get; set; }
        public string OptionValue { get; set; } = null!;
    }
}
