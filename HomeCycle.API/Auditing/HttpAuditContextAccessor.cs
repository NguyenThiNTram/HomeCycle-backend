using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;

namespace HomeCycle.API.Auditing
{
    public sealed class HttpAuditContextAccessor : IAuditContextAccessor
    {
        private const int MaxUserAgentLength = 512;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogOptions _options;
        private readonly IWebHostEnvironment _environment;

        public HttpAuditContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuditLogOptions> options,
            IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _environment = environment;
        }

        public AuditContext Capture()
        {
            var context = _httpContextAccessor.HttpContext;

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

            if (Enum.TryParse<UserRole>(roleValue, true, out var parsedRole))
                userRole = parsedRole;

            var correlationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

            string? ipAddress = null;

            if (_options.Context.CaptureIpAddress)
                ipAddress = ResolveClientIpAddress(context);

            string? userAgent = null;

            if (_options.Context.CaptureUserAgent)
            {
                var rawUserAgent = context.Request.Headers["User-Agent"].ToString();

                if (!string.IsNullOrWhiteSpace(rawUserAgent))
                {
                    userAgent = rawUserAgent.Length <= MaxUserAgentLength
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

        private string? ResolveClientIpAddress(HttpContext context)
        {
            if (_environment.IsProduction())
            {
                var cloudflareIp = context.Request.Headers["CF-Connecting-IP"].ToString();

                if (IPAddress.TryParse(cloudflareIp, out var parsedCloudflareIp))
                    return NormalizeIpAddress(parsedCloudflareIp);

                return null;
            }

            var remoteIp = context.Connection.RemoteIpAddress;

            return remoteIp is null
                ? null
                : NormalizeIpAddress(remoteIp);
        }

        private static string NormalizeIpAddress(IPAddress ipAddress)
        {
            return ipAddress.IsIPv4MappedToIPv6
                ? ipAddress.MapToIPv4().ToString()
                : ipAddress.ToString();
        }
    }
}
