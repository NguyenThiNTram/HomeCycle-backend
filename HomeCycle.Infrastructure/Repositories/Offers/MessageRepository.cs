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
    public class MessageRepository : IMessageRepository
    {
        private readonly HomeCycleDbContext _db;

        public MessageRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<message?> GetPendingCounterOfferByNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Messages
                .AsNoTracking()
                .Where(x => x.NegotiationId == negotiationId
                    && x.MessageType == (int)HomeCycle.Domain.Enums.MessageType.CounterOffer
                    && x.OfferStatus == (int)HomeCycle.Domain.Enums.OfferStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(message entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Messages.AddAsync(infraEntity, cancellationToken);
        }

        public Task UpdateAsync(message entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            _db.Messages.Update(infraEntity);
            return Task.CompletedTask;
        }
    }
}
