using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Posts
{
    public class PostSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? ProductTypeId { get; set; }

        public Guid? BrandId { get; set; }

        public string? City { get; set; }

        public PostType? PostType { get; set; }

        public PostStatus? Status { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public bool? IsBusinessPosting { get; set; }
    }
}
