using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.Interfaces.Services.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;

        public WalletController(IWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        // User tạo yêu cầu rút tiền cho chính ví của mình.
        [HttpPost("withdrawals")]
        public async Task<IActionResult> CreateWithdrawal(
           [FromBody] CreateWithdrawalRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _withdrawalService.CreateWithdrawalRequestAsync(
                currentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { withdrawalId = result.Data });
        }

        // User (hoặc FE tự động) chủ động đồng bộ trạng thái 1 yêu cầu rút tiền đang Processing.
        [HttpPost("withdrawals/{withdrawalId:guid}/sync")]
        public async Task<IActionResult> SyncWithdrawalStatus(
            Guid withdrawalId, CancellationToken cancellationToken)
        {
            var result = await _withdrawalService.SyncWithdrawalStatusAsync(withdrawalId, cancellationToken);

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
