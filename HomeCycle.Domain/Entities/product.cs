using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Domain.Entities;

public class product
{
    public Guid ProductId { get; set; }
    public Guid PostId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid ProductTypeId { get; set; }
    public Guid? BrandId { get; set; }

    public string? ProductName { get; set; }
    //public string? BrandName { get; set; }
    public string? SpaceUsage { get; set; }
    public string? ModelNumber { get; set; }
    public decimal? OriginalPrice { get; set; }

    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }

    public FunctionalityStatus? FunctionalityStatus { get; set; }
    public int? UsageDuration { get; set; }
    public int? DamageLevel { get; set; }
    public string? DetailDescription { get; set; }

    public product()
    {
    }

    public product(Guid ProductId, Guid PostId, Guid CategoryId, Guid ProductTypeId, Guid? BrandId)
    {
        this.ProductId = ProductId;
        this.PostId = PostId;
        this.CategoryId = CategoryId;
        this.ProductTypeId = ProductTypeId;
        this.BrandId = BrandId;
    }
}
