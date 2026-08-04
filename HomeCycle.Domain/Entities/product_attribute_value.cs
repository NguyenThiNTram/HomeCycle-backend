using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class product_attribute_value
{
    public Guid ProductId { get; set; }
    public Guid AttributeId { get; set; }
    public Guid? OptionId { get; set; }

    public InputType? InputType { get; set; }
    public bool? ValueBoolean { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }

    public string? AttributeName { get; set; }
    public Enums.DataType? DataType { get; set; }
    public string? Unit { get; set; }
    public string? OptionValue { get; set; }
    public int? AttributeDisplayOrder { get; set; }
    public int? OptionDisplayOrder { get; set; }

    public product_attribute_value()
    {
    }

    public product_attribute_value(Guid ProductId, Guid AttributeId)
    {
        this.ProductId = ProductId;
        this.AttributeId = AttributeId;
    }
}
