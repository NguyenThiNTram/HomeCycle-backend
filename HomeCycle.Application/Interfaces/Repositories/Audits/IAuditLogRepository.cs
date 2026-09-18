using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Audits;
using HomeCycle.Application.DTOs.Responses.Audits;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Audits
{
    public interface IAuditLogRepository
    {
        Task<PagedResult<AuditLogListItemResponse>> GetPagedAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default);
        Task<audit_log?> GetByIdAsync(Guid auditId, CancellationToken cancellationToken = default);
    }
}
