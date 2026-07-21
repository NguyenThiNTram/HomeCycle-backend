using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class product_attribute
{
    public Guid AttributeId { get; set; }
    public Guid ProductTypeId { get; set; }

    public string? AttributeName { get; set; }
    public int? DataType { get; set; }
    public string? Unit { get; set; }
    public int? DisplayOrder { get; set; }

    public bool IsFilterable { get; set; }
    public bool IsRequired { get; set; }

    public product_attribute()
    {
    }

    public product_attribute(Guid AttributeId, Guid ProductTypeId)
    {
        this.AttributeId = AttributeId;
        this.ProductTypeId = ProductTypeId;
    }

    public ICollection<product_attribute_option> ProductAttributeOptions { get; set; } = new List<product_attribute_option>();
}
