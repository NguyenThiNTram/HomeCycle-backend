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
    }
}
