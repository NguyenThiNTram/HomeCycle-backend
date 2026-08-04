using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Offers
{
    public class NegotiationRepository : INegotiationRepository
    {
        private readonly HomeCycleDbContext _db;

        public NegotiationRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<negotiation?> GetByOfferIdAsync(Guid offerId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Negotiations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<negotiation?> GetByIdAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Negotiations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NegotiationId == negotiationId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(negotiation entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Negotiations.AddAsync(infraEntity, cancellationToken);
        }

        public Task UpdateAsync(negotiation entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            _db.Negotiations.Update(infraEntity);
            return Task.CompletedTask;
        }
    }
}
