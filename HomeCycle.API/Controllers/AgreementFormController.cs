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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var result = await _agreementService.GetPreviewAsync(negotiationId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var result = await _agreementService.CreateAgreementAsync(request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { AgreementId = result.Data });
        }

        [HttpGet("{agreementId}")]
        public async Task<IActionResult> GetDetail(Guid agreementId, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var result = await _agreementService.GetDetailAsync(agreementId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPut("{agreementId}")]
        public async Task<IActionResult> UpdateAgreement(Guid agreementId, [FromBody] UpdateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var result = await _agreementService.UpdateAgreementAsync(agreementId, request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok();
        }

        [HttpPatch("{id}/accept")]
        public async Task<IActionResult> AcceptAgreement(Guid id, CancellationToken cancellationToken)
        {
            // Lấy user ID theo chuẩn chung của controller hiện tại
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var result = await _agreementService.AcceptAgreementAsync(id, currentUserId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(new { Message = "Chấp nhận thỏa thuận thành công. Vui lòng tiến hành thanh toán." });
        }

    }
}
