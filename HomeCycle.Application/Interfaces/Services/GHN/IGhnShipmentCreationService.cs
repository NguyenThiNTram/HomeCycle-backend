using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.GHN
{
    public interface IGhnShipmentCreationService
    {
        Task<HomeCycle.Application.Commons.Results.Result> CancelForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task CancelForOrderSafelyAsync(Guid orderId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Quét và xử lý các vận đơn GHN đang chờ (Pending/Failed + Processing kẹt quá lâu):
        /// claim -> gọi CreateOrderAsync -> lưu GHNOrderCode.
        /// </summary>
        Task<int> ProcessPendingAsync(int batchSize, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default);
    }
}
