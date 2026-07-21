using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HomeCycle.Application.DTOs.Requests.Products
{
    public class ProductRequirementRequest
    {
        public Guid CategoryId { get; set; }

        public Guid ProductTypeId { get; set; }

        public Guid? BrandId { get; set; }

        public decimal? ExpectedPrice { get; set; }

        public string? ProductName { get; set; }

        public string? SpaceUsage { get; set; }

        public FunctionalityStatus? FunctionalityStatus { get; set; }

        public int? UsageDuration { get; set; }

        public int? DamageLevel { get; set; }

        //public List<CreateAttributeRequest> Attributes { get; set; } = new();
        //tham chiếu attribute có sẵn + giá trị mong muốn, tái dùng đúng type đã có
        public List<ProductAttributeValueRequest> AttributeValues { get; set; } = new();

    }
}
