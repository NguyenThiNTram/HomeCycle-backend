using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Shipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Shipments
{
    public interface IShipmentService
    {
        Task<Result<ShipmentSellerReadyResponseDto>> ConfirmSellerReadyAsync(
            Guid shipmentId,
            Guid sellerId,
            CancellationToken ct = default);
    }
}
