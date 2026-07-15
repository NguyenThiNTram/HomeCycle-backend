using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Profiles
{
    public interface IBusinessProductTypeRepository
    {
        Task AddAsync(business_product_type productType, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<business_product_type> productTypes, CancellationToken cancellationToken = default);
        Task<List<business_product_type>> GetByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
        Task DeleteAllByProfileIdAsync(Guid businessProfileId, CancellationToken cancellationToken = default);
    }
}

