using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Audits
{
    public sealed record AuditContext(
        bool IsHttpRequest,
        Guid? UserId,
        UserRole? UserRole,
        string? CorrelationId,
        string? IpAddress,
        string? UserAgent)
    {
        public static AuditContext Empty { get; } =
            new(false, null, null, null, null, null);
    }
}
