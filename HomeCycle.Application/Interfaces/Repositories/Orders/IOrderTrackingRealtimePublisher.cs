using HomeCycle.Application.DTOs.Responses.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Orders
{
    public interface IOrderTrackingRealtimePublisher
    {
        Task PublishUpdatedAsync(
            Guid orderId,
            OrderTrackingUpdatedResponse response,
            CancellationToken cancellationToken = default);
    }
}
