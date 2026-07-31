using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class AttributeFilterRequest
    {
        public Guid AttributeId { get; set; }

        /// Attribute có Option (Dropdown/RadioButton) — chọn nhiều Option cùng lúc (OR logic)
        public List<Guid>? OptionIds { get; set; }

        /// Attribute là Number — lọc theo khoảng
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        /// Attribute là Boolean
        public bool? ValueBoolean { get; set; }

        /// Attribute là Text tự do
        public string? ValueTextContains { get; set; }
    }
}
