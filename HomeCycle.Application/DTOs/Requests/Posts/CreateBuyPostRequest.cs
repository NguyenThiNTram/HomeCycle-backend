using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class CreateBuyPostRequest : CreatePostRequest
    {
        public decimal ExpectedPrice { get; set; }
        public ProductRequirementRequest Requirement { get; set; } = default!;
    }

    public class UpdateBuyPostRequest : CreatePostRequest
    {
        public decimal ExpectedPrice { get; set; }
        public ProductRequirementRequest Requirement { get; set; } = default!;
    }
}
