using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Orders
{
    public interface IOrderTrackingRealtimeService
    {
        Task PublishByOrderIdSafelyAsync(Guid orderId, DateTime updatedAt);
        Task PublishByAgreementIdSafelyAsync(Guid agreementId, DateTime updatedAt);
    }
}
