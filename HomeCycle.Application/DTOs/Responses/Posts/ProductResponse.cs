using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Posts
{
    public sealed class BuyProductRequirementResponse
    {
        public BuyProductRequirementResponse(ProductResponse source)
        {
            ProductId = source.ProductId;
            CategoryId = source.CategoryId;
            ProductTypeId = source.ProductTypeId;
            BrandId = source.BrandId;
            CategoryName = source.CategoryName;
            ProductTypeName = source.ProductTypeName;
            BrandName = source.BrandName;
            ProductName = source.ProductName;
            ModelNumber = source.ModelNumber;
            FunctionalityStatus = source.FunctionalityStatus;
            UsageDuration = source.UsageDuration;
            DamageLevel = source.DamageLevel;
            AttributeValues = source.AttributeValues;
        }

        public Guid ProductId { get; }
        public Guid? CategoryId { get; }
        public Guid? ProductTypeId { get; }
        public Guid? BrandId { get; }
        public string? CategoryName { get; }
        public string? ProductTypeName { get; }
        public string? BrandName { get; }
        public string? ProductName { get; }
        public string? ModelNumber { get; }
        public FunctionalityStatus? FunctionalityStatus { get; }
        public int? UsageDuration { get; }
        public DamageLevel? DamageLevel { get; }
        public IReadOnlyList<ProductAttributeValueResponse> AttributeValues { get; }
    }

    public class ProductResponse
    {
        public Guid ProductId { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? ProductTypeId { get; set; }

        public Guid? BrandId { get; set; }

        public string? CategoryName { get; set; }

        public string? ProductTypeName { get; set; }

        public string? BrandName { get; set; }

        public string? ProductName { get; set; }

        public SpaceUsage? SpaceUsage { get; set; }

        public string? ModelNumber { get; set; }

        public decimal? OriginalPrice { get; set; }

        public decimal? Length { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public decimal? Weight { get; set; }

        public FunctionalityStatus? FunctionalityStatus { get; set; }

        public int? UsageDuration { get; set; }

        public DamageLevel? DamageLevel { get; set; }

        public string? DetailDescription { get; set; }

        public IReadOnlyList<ProductAttributeValueResponse> AttributeValues { get; set; }
            = new List<ProductAttributeValueResponse>();
    }
}
