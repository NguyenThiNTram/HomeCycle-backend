using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.GHN
{
    public interface IGhnTrackingSyncService
    {
        Task<Result<ShipmentTrackingResponse>> SyncByOrderIdAsync(
            Guid orderId,
            Guid currentUserId,
            CancellationToken cancellationToken = default);
    }
}
