using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class ShipmentTrackingResponse
    {
        public Guid OrderId { get; init; }

        public Guid ShipmentId { get; init; }

        public DeliveryMethod DeliveryMethod { get; init; }

        public GHNCreationStatus CreationStatus { get; init; }

        public string? TrackingCode { get; init; }
        public string? CarrierStatus { get; init; }

        public ShipmentStatus? ShipmentStatus { get; init; }

        public DateTime? ExpectedDeliveryAt { get; init; }

        public DateTime? DeliveredAt { get; init; }

        // Lần đồng bộ GHN thành công gần nhất.
        public DateTime? LastSyncedAt { get; init; }

        // True khi lần gọi GHN hiện tại thất bại và response đang dùng dữ liệu DB.
        public bool IsStale { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
