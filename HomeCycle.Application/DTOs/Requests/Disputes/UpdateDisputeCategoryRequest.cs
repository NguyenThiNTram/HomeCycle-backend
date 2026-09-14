using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class UpdateDisputeCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<DisputeTargetType>? TargetTypes { get; set; }
    }
}
