using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Products
{
    public class ProductAttributeValueResponse
    {
        public Guid AttributeId { get; set; }

        public string? AttributeName { get; set; }

        public DataType? DataType { get; set; }

        public string? Unit { get; set; }

        public Guid? OptionId { get; set; }

        public string? OptionValue { get; set; }

        public InputType? InputType { get; set; }

        public bool? ValueBoolean { get; set; }

        public decimal? ValueNumber { get; set; }

        public string? ValueText { get; set; }
    }
}
