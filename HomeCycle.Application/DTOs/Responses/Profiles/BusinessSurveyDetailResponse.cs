using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Profiles
{
    public class BusinessSurveyDetailResponse
    {
        public List<string> TargetCities { get; set; } = new();
        public List<int> AcceptableDamageLevels { get; set; } = new();
        public List<int> AcceptableFunctionalityStatuses { get; set; } = new();
        public List<int> ProcurementScales { get; set; } = new();
        public List<Guid> ProductTypeIds { get; set; } = new(); 
    }
}
