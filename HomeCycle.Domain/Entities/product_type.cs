using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class product_type
{
    public Guid ProductTypeId { get; set; }
    public Guid CategoryId { get; set; }

    public string? ProductTypeName { get; set; }
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public product_type()
    {
    }

    public product_type(Guid ProductTypeId, Guid CategoryId)
    {
        this.ProductTypeId = ProductTypeId;
        this.CategoryId = CategoryId;
    }

    public ICollection<product_attribute> ProductAttributes { get; set; } = new List<product_attribute>();
}

