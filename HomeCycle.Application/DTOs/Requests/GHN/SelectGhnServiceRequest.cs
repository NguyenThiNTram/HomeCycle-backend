using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public sealed class SelectGhnServiceRequest
    {
        public int ServiceId { get; init; }
        public int ServiceTypeId { get; init; }

        public int PaymentTypeId { get; init; }

        public required string RequiredNote { get; init; }
        public string? Note { get; init; }
    }
}
