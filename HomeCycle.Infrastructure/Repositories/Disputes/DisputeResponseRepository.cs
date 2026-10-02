using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Disputes
{
    public class DisputeResponseRepository : IDisputeResponseRepository
    {
        private readonly HomeCycleDbContext _db;

        public DisputeResponseRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(dispute_response response, CancellationToken ct = default)
        {
            await _db.Dispute_Responses.AddAsync(response.ToInfrastructure(), ct);
        }

        public Task<bool> ExistsByResponderAsync(
            Guid disputeId,
            Guid responderId,
            CancellationToken ct = default)
        {
            return _db.Dispute_Responses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.DisputeId == disputeId &&
                    x.ResponderId == responderId,
                    ct);
        }

        public async Task<IReadOnlyList<dispute_response>> GetByDisputeIdAsync(
            Guid disputeId,
            CancellationToken ct = default)
        {
            var entities = await _db.Dispute_Responses
                .AsNoTracking()
                .Where(x => x.DisputeId == disputeId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }
    }
}
