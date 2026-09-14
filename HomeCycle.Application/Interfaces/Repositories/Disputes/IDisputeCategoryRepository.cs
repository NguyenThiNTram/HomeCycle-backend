using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Disputes
{
    public interface IDisputeCategoryRepository
    {
        Task<IReadOnlyList<dispute_category>> GetAllAsync(bool? isActive = null, CancellationToken ct = default);
        Task<IReadOnlyList<dispute_category>> GetActiveByTargetTypeAsync(DisputeTargetType targetType, CancellationToken ct = default);
        Task<dispute_category?> GetByIdAsync(int categoryId, CancellationToken ct = default);
        Task<dispute_category?> GetByIdForUpdateAsync(int categoryId, CancellationToken ct = default);
        Task<dispute_category?> GetByCodeAsync(string code, CancellationToken ct = default);
        Task<bool> ExistsCodeAsync(string code, CancellationToken ct = default);
        Task AddAsync(dispute_category category, CancellationToken ct = default);
        Task UpdateAsync(dispute_category category, CancellationToken ct = default);
        Task ReplaceTargetsAsync(int categoryId, IReadOnlyCollection<DisputeTargetType> targetTypes, CancellationToken ct = default);
    }
}
