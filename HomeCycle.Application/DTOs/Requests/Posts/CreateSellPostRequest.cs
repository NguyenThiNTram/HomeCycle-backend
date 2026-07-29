using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class CreateSellPostRequest : CreatePostRequest
    {
        public decimal BasePrice { get; set; }
        public ProductRequest Product { get; set; } = default!;
    }

    public class UpdateSellPostRequest : CreatePostRequest
    {
        public decimal BasePrice { get; set; }
        public ProductRequest Product { get; set; } = default!;
    }
}
