using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Profiles
{
    public interface IBusinessDocumentRepository
    {
        Task AddAsync(business_document document, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<business_document> documents, CancellationToken cancellationToken = default);
        Task<List<business_document>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
        void UpdateRange(IEnumerable<business_document> documents);
        Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
        Task<List<business_document>> GetActiveByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
        Task<business_document> GetActiveByProfileIdAndTypeAsync(Guid businessProfileId, int documentType, CancellationToken cancellationToken = default);

        void Update(business_document document);
    }
}
