using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Audits;
using HomeCycle.Application.DTOs.Responses.Audits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Audits
{
    public interface IAuditService
    {
        Task EnqueueAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default);
        Task<Result<PagedResult<AuditLogListItemResponse>>> GetPagedAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuditLogDetailResponse>> GetByIdAsync(Guid auditId, CancellationToken cancellationToken = default);
    }
}
