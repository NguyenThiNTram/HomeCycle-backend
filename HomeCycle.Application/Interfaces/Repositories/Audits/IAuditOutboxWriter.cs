using HomeCycle.Application.Commons.Audits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Audits
{
    public interface IAuditOutboxWriter
    {
        Task EnqueueAsync(
            AuditRecord auditRecord,
            CancellationToken cancellationToken = default);
    }
}
