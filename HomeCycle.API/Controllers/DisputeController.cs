using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Route("api/disputes")]
    [Authorize]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateDisputeRequest request, CancellationToken cancellationToken)
        {
            var result = await _disputeService.CreateAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return CreatedAtAction(
                nameof(GetDetail),
                new { disputeId = result.Data!.DisputeId },
                result.Data);
        }

        [HttpGet("{disputeId:guid}")]
        public async Task<IActionResult> GetDetail(Guid disputeId, CancellationToken cancellationToken)
        {
            var result = await _disputeService.GetDetailForUserAsync(disputeId, CurrentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        private Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                    throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

                return userId;
            }
        }
    }
}
