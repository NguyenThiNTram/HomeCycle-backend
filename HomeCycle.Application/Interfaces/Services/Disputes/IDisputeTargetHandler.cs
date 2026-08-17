using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Disputes
{
    public interface IDisputeTargetHandler
    {
        DisputeTargetType TargetType { get; }

        Task<Result<DisputeTargetCreateContext>>
            PrepareCreateAsync(
                Guid senderId,
                Guid targetId,
                DisputeCategory category,
                DateTime nowUtc,
                CancellationToken cancellationToken = default);

        Task<Result<DisputeTargetSummaryDto>>
            BuildSummaryAsync(
                dispute dispute,
                CancellationToken cancellationToken = default);
    }

    public class DisputeTargetCreateContext
    {
        public DisputeTargetType TargetType { get; init; }

        public Guid TargetId { get; init; }

        public Guid TargetUserId { get; init; }

        public Guid? OrderId { get; init; }

        public Guid? ReviewId { get; init; }

        public DateTime? DisputeDeadlineUtc { get; init; }
    }
}
