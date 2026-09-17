using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Audits
{
    public interface IAuditRetentionProcessor
    {
        Task<(int AuditLogsDeleted, int FailedOutboxesDeleted)> CleanupAsync(CancellationToken cancellationToken = default);
    }
}
