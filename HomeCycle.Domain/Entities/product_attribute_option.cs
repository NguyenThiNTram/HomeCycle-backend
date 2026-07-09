using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class product_attribute_option
{
    public Guid OptionId { get; set; }
    public Guid AttributeId { get; set; }

    public string? OptionValue { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsDefault { get; set; }

    public product_attribute_option()
    {
    }

    public product_attribute_option(Guid OptionId, Guid AttributeId)
    {
        this.OptionId = OptionId;
        this.AttributeId = AttributeId;
    }
}
