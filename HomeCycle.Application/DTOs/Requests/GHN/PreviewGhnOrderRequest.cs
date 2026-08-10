using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.GHN
{
    public class PreviewGhnOrderRequest
    {
        // 1 = seller; 2 = buyer
        public int PaymentTypeId { get; init; }

        public required string RequiredNote { get; init; }
    }
}
