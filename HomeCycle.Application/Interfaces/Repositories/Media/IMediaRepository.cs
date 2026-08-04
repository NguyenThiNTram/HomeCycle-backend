using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Media
{
    public interface IMediaRepository
    {
        Task AddRangeAsync(IEnumerable<media> entities, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<media>> GetByTargetAsync(Guid targetId, string targetType, CancellationToken cancellationToken = default);

        Task RemoveByTargetAsync(Guid targetId, string targetType, CancellationToken cancellationToken = default);
    }
}
