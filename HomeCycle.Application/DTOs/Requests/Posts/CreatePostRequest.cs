using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class CreatePostRequest
    {
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string? StreetAddress { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public string? PriorityLevel { get; set; }
        public bool IsBusinessPosting { get; set; }
        public DateTime? ExpiryDate { get; set; }

        //public IList<MediaRequest> Medias { get; set; } = new List<MediaRequest>();
        public List<IFormFile>? Medias { get; set; }
    }
}
