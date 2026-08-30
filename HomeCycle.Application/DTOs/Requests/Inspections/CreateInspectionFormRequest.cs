using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Inspections
{
    public class CreateInspectionFormRequest
    {
        public InspectionOperatingStatus? OperatingStatus { get; set; }
        public InspectionAppearanceStatus? AppearanceStatus { get; set; }
        public InspectionPartsStatus? PartsStatus { get; set; }
        public InspectionMatchStatus? MatchStatus { get; set; }

        public string? InspectorNotes { get; set; }

        public InspectionConclusion? Conclusion { get; set; }
        public decimal? SuggestedPrice { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
