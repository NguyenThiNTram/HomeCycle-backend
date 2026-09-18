using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;

namespace HomeCycle.API.Auditing
{
    public sealed class HttpAuditContextAccessor
        : IAuditContextAccessor
    {
        private const int MaxUserAgentLength = 512;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogOptions _options;

        public HttpAuditContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuditLogOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public AuditContext Capture()
        {
            var context =
                _httpContextAccessor.HttpContext;

            if (context is null)
                return AuditContext.Empty;

            Guid? userId = null;

            var userIdValue = context.User
                .FindFirst(ClaimTypes.NameIdentifier)
                ?.Value;

            if (Guid.TryParse(userIdValue, out var parsedUserId))
                userId = parsedUserId;

            UserRole? userRole = null;

            var roleValue = context.User
                .FindFirst(ClaimTypes.Role)
                ?.Value;

            if (Enum.TryParse<UserRole>(
                roleValue,
                true,
                out var parsedRole))
            {
                userRole = parsedRole;
            }

            var correlationId =
                Activity.Current?.TraceId.ToString()
                ?? context.TraceIdentifier;

            string? ipAddress = null;

            if (_options.Context.CaptureIpAddress)
            {
                ipAddress = context.Connection
                    .RemoteIpAddress
                    ?.ToString();
            }

            string? userAgent = null;

            if (_options.Context.CaptureUserAgent)
            {
                var rawUserAgent =
                    context.Request.Headers["User-Agent"]
                        .ToString();

                if (!string.IsNullOrWhiteSpace(rawUserAgent))
                {
                    userAgent =
                        rawUserAgent.Length <= MaxUserAgentLength
                            ? rawUserAgent
                            : rawUserAgent[..MaxUserAgentLength];
                }
            }

            return new AuditContext(
                true,
                userId,
                userRole,
                correlationId,
                ipAddress,
                userAgent);
        }
    }
}
