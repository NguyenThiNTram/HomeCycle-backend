using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Disputes
{
    public interface IDisputeResponseRepository
    {
        Task AddAsync(dispute_response response, CancellationToken ct = default);

        Task<bool> ExistsByResponderAsync(
            Guid disputeId,
            Guid responderId,
            CancellationToken ct = default);

        Task<IReadOnlyList<dispute_response>> GetByDisputeIdAsync(
            Guid disputeId,
            CancellationToken ct = default);
    }
}
