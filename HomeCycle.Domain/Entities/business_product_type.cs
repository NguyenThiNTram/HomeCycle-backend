using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;
public class business_product_type
{
    public Guid BusinessProductTypeId { get; set; }
    public Guid BusinessProfileId { get; set; }
    public Guid ProductTypeId { get; set; }

    public int? Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public business_product_type()
    {
    }

    public business_product_type(Guid BusinessProductTypeId, Guid BusinessProfileId, Guid ProductTypeId)
    {
        this.BusinessProductTypeId = BusinessProductTypeId;
        this.BusinessProfileId = BusinessProfileId;
        this.ProductTypeId = ProductTypeId;
    }
}
