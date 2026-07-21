using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class ProductRequest
    {
        public Guid CategoryId { get; set; }

        public Guid ProductTypeId { get; set; }

        public Guid? BrandId { get; set; }

        public string? ProductName { get; set; }

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

        public IList<ProductAttributeValueRequest> AttributeValues { get; set; }
            = new List<ProductAttributeValueRequest>();
    }
}
