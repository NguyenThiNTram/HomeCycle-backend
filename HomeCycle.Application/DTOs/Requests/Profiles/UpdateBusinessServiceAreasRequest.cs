using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Profiles
{
    public class UpdateBusinessServiceAreasRequest
    {
        public List<BusinessServiceAreaRequestDto> ServiceAreas { get; set; } = new();
    }

    public class BusinessServiceAreaRequestDto
    {
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
    }
}
