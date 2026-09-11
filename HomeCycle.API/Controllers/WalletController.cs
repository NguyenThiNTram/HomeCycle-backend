using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly IWalletService _walletService;

        public WalletController(IWithdrawalService withdrawalService, IWalletService walletService)
        {
            _withdrawalService = withdrawalService;
            _walletService = walletService;
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

        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var walletType = ResolveWalletType();

            var result = await _walletService.GetMyWalletAsync(userId, walletType, ct);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("me/ledger")]
        public async Task<IActionResult> GetMyWalletStatement([FromQuery] WalletLedgerSearchRequest request, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var walletType = ResolveWalletType();

            var result = await _walletService.GetWalletStatementAsync(userId, walletType, request, ct);
            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result.Data);
        }

        // Chỉ Moderator/Admin xem được tổng tiền hệ thống đang giữ hộ
        [HttpGet("system")]
        [Authorize(Roles = nameof(UserRole.Moderator) + "," + nameof(UserRole.Admin))]
        public async Task<IActionResult> GetSystemWalletSummary(CancellationToken ct)
        {
            var result = await _walletService.GetSystemWalletSummaryAsync(ct);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("withdrawals")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử yêu cầu rút tiền của người dùng",
            Description = "Trả về danh sách Withdrawal thuộc người dùng hiện tại, hỗ trợ lọc theo trạng thái, khoảng thời gian và phân trang."
        )]
        [ProducesResponseType(typeof(PagedResult<WithdrawalListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyWithdrawals(
            [FromQuery] WithdrawalSearchRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result = await _withdrawalService.GetMyWithdrawalsAsync(
                userId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("withdrawals/{withdrawalId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết yêu cầu rút tiền của người dùng",
            Description = "Trả về chi tiết một Withdrawal thuộc người dùng hiện tại, gồm trạng thái, tài khoản ngân hàng, lý do từ chối và các financial event liên quan."
        )]
        [ProducesResponseType(typeof(WithdrawalDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyWithdrawalDetail(
            Guid withdrawalId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            var result = await _withdrawalService.GetMyWithdrawalDetailAsync(
                userId,
                withdrawalId,
                cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Error?.Code == "Withdrawal.NotFound")
                    return NotFound(result.Error);

                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        private WalletTypeEnum ResolveWalletType()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (Enum.TryParse<UserRole>(roleClaim, out var role) && role == UserRole.Business)
                return WalletTypeEnum.Business;

            return WalletTypeEnum.Personal;
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
