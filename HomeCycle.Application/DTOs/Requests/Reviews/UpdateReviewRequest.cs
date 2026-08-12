using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Reviews
{
    public class UpdateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
