using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed record GhnCreateOrderResponse(string OrderCode, decimal TotalFee, decimal ServiceFee, decimal CodFee, DateTimeOffset? ExpectedDeliveryAt);
}
