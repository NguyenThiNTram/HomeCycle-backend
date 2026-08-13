using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class GhnOrderDetailResponse
    {
        public required string OrderCode { get; init; }

        public string? ClientOrderCode { get; init; }

        public required string CarrierStatus { get; init; }

        public int? ServiceTypeId { get; init; }

        public int? WeightGram { get; init; }
        public int? ConvertedWeightGram { get; init; }

        public int? LengthCm { get; init; }
        public int? WidthCm { get; init; }
        public int? HeightCm { get; init; }

        public string? RequiredNote { get; init; }
        public string? Content { get; init; }
        public string? Note { get; init; }

        public DateTimeOffset? ExpectedDeliveryAt { get; init; }
        public DateTimeOffset? OrderCreatedAt { get; init; }
        public DateTimeOffset? FinishedAt { get; init; }
        public DateTimeOffset? CarrierUpdatedAt { get; init; }

        public IReadOnlyList<GhnTrackingLogResponse> Timeline { get; init; }
            = Array.Empty<GhnTrackingLogResponse>();
    }

    public sealed class GhnTrackingLogResponse
    {
        public required string Status { get; init; }
        public DateTimeOffset? OccurredAt { get; init; }
    }
}
