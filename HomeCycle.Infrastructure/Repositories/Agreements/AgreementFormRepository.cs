using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Agreements
{
    public class AgreementFormRepository : IAgreementFormRepository
    {
        private readonly HomeCycleDbContext _db;

        public AgreementFormRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<agreement_form?> GetByNegotiationIdAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Agreement_Forms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NegotiationId == negotiationId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<agreement_form?> GetByIdAsync(Guid agreementId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Agreement_Forms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AgreementId == agreementId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(agreement_form agreement, CancellationToken cancellationToken = default)
        {
            var entity = agreement.ToInfrastructure();
            await _db.Agreement_Forms.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(agreement_form agreement, CancellationToken cancellationToken = default)
        {
            var entity = agreement.ToInfrastructure();
            _db.Agreement_Forms.Update(entity);
            return Task.CompletedTask;
        }

    }
}
