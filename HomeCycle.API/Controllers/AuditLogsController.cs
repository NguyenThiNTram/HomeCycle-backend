using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.DTOs.Requests.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public sealed class AuditLogsController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách audit log",
            Description = "Admin tra cứu audit log theo thời gian, actor, action, target, source và outcome.")]
        public async Task<IActionResult> GetAuditLogsAsync(
            [FromQuery] AuditLogSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _auditService.GetPagedAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("{auditId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết audit log",
            Description = "Admin xem đầy đủ context và payload thay đổi của một audit event.")]
        public async Task<IActionResult> GetAuditLogDetailAsync(
            Guid auditId,
            CancellationToken cancellationToken)
        {
            var result = await _auditService.GetByIdAsync(auditId, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == AuditErrors.NotFound.Code)
                    return NotFound(result.Error);

                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }
    }
}
