using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Profiles
{
    public class BusinessDocumentRepository : IBusinessDocumentRepository
    {
        private readonly HomeCycleDbContext _db;

        public BusinessDocumentRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(business_document document, CancellationToken cancellationToken = default)
        {
            var entity = document.ToInfrastructure();

            
            await _db.Business_Documents.AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<business_document> documents, CancellationToken cancellationToken = default)
        {
            var entities = documents.Select(doc => doc.ToInfrastructure()).ToList();
            await _db.Business_Documents.AddRangeAsync(entities, cancellationToken);
        }

        public async Task<List<business_document>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var entities = await _db.Business_Documents
                .AsNoTracking()
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public void UpdateRange(IEnumerable<business_document> documents)
        {
            var entities = documents.Select(doc => doc.ToInfrastructure()).ToList();
            _db.Business_Documents.UpdateRange(entities);
        }

        public async Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default)
        {
            var documents = await _db.Business_Documents
                .Where(x => x.BusinessProfileId == businessProfileId)
                .ToListAsync(cancellationToken);

            if (documents.Any())
            {
                _db.Business_Documents.RemoveRange(documents);
            }
        }
    }
}
