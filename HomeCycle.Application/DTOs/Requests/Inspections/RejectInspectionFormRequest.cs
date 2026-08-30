using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Inspections
{
    public class RejectInspectionFormRequest
    {
        public int ExpectedRevision { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
