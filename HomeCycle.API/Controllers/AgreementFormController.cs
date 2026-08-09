using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.Interfaces.Services.Agreements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/agreements")]
    [Authorize]
    public class AgreementFormController : ControllerBase
    {
        private readonly IAgreementFormService _agreementService;

        public AgreementFormController(IAgreementFormService agreementService)
        {
            _agreementService = agreementService;
        }

        [HttpGet("preview/{negotiationId}")]
        public async Task<IActionResult> GetPreview(Guid negotiationId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetPreviewAsync(negotiationId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.CreateAgreementAsync(request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { AgreementId = result.Data });
        }

        [HttpGet("{agreementId}")]
        public async Task<IActionResult> GetDetail(Guid agreementId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetDetailAsync(agreementId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPut("{agreementId}")]
        public async Task<IActionResult> UpdateAgreement(Guid agreementId, [FromBody] UpdateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.UpdateAgreementAsync(agreementId, request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPatch("{id}/accept")]
        public async Task<IActionResult> AcceptAgreement(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.AcceptAgreementAsync(id, currentUserId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPatch("{id}/request-edit")]
        public async Task<IActionResult> RequestEdit(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.RequestEditAsync(id, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }


        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

            return userId;
        }
    }
}
