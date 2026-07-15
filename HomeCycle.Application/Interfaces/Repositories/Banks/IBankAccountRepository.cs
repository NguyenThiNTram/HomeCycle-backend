using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Banks
{
    public interface IBankAccountRepository
    {
        Task AddAsync(bank_account bankAccount, CancellationToken cancellationToken = default);
        Task<bank_account?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        void Update(bank_account bankAccount);
    }
}
