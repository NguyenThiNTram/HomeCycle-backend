using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Moderators
{
    public class PendingBusinessProfileSummaryDto
    {
        public Guid BusinessProfileId { get; set; }
        public string BusinessName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
