using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Wallets
{
    public interface IWithdrawalRepository
    {
        Task AddAsync(withdrawal withdrawal, CancellationToken ct = default);
        Task<withdrawal?> GetByIdAsync(Guid withdrawalId, CancellationToken ct = default);
        Task UpdateAsync(withdrawal withdrawal, CancellationToken ct = default);
        Task<withdrawal?> GetByIdForUpdateAsync(
            Guid withdrawalId,
            CancellationToken ct = default);
        Task<PagedResult<WithdrawalReadModel>> GetPagedAsync(
            WithdrawalQuery query,
            CancellationToken ct = default);

        Task<WithdrawalReadModel?> GetReadModelByIdAsync(
            Guid withdrawalId,
            CancellationToken ct = default);
    }
}
