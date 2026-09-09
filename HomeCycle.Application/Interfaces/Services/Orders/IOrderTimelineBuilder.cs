using HomeCycle.Application.DTOs.Responses.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Orders
{
    public interface IOrderTimelineBuilder
    {
        IReadOnlyList<OrderTimelineStepDto> Build(OrderDetailDto detail, bool isInspectionCollectNow);
    }
}
