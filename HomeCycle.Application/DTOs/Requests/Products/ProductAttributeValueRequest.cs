using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class ProductAttributeValueRequest
    {
        public Guid AttributeId { get; set; }
        public Guid? OptionId { get; set; }
        public InputType? InputType { get; set; }
        public bool? ValueBoolean { get; set; }
        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
    }
}
