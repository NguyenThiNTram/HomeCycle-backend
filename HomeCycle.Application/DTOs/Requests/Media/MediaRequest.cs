using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Media
{

    public class MediaRequest
    {
        public IFormFile File { get; set; } = null!;
        public int DisplayOrder { get; set; } = 1;
        //public bool IsPrimary { get; set; } = false;
    }
}
