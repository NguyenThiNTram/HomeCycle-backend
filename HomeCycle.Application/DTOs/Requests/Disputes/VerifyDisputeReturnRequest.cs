using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Disputes
{
    public class VerifyDisputeReturnRequest
    {
        public bool IsReturnCompleted { get; set; }
        public string ModeratorNote { get; set; } = string.Empty;
    }
}
