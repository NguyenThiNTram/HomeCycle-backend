using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/moderator")]
    [ApiController]
    [Authorize(Roles = "Moderator")]
    public class ModeratorController : ControllerBase
    {
        private readonly IModeratorService _moderatorService;
        private readonly IPostService _postService;
        private readonly IWithdrawalService _withdrawalService;
        private readonly IDisputeService _disputeService;
        public ModeratorController(IModeratorService moderatorService, IPostService postService, IWithdrawalService withdrawalService, IDisputeService disputeService)
        {
            _moderatorService = moderatorService;
            _postService = postService;
            _withdrawalService = withdrawalService;
            _disputeService = disputeService;
        }

        [HttpPost("business-profiles/review")]
        public async Task<IActionResult> ReviewBusinessProfile(
            [FromBody] ReviewBusinessProfileRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _moderatorService.ReviewBusinessProfileAsync(moderatorId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Data
            });
        }

        [HttpGet("business-profiles/{profileId}")]
        public async Task<IActionResult> GetBusinessProfileDetail(
            [FromRoute] Guid profileId,
            CancellationToken cancellationToken)
        {
            var result = await _moderatorService.GetBusinessProfileDetailForModeratorAsync(profileId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpGet("business-profiles/pending")]
        public async Task<IActionResult> GetPendingBusinessProfiles(
            [FromQuery] string? keyword,
            CancellationToken cancellationToken)
        {
            var result = await _moderatorService.GetPendingBusinessProfilesAsync(keyword, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpGet("personal-profiles/pending")]
        public async Task<IActionResult> GetPending( [FromQuery] string? keyword,CancellationToken cancellationToken)
        {
            var result =
                await _moderatorService
                    .GetPendingPersonalVerificationsAsync(
                        keyword,
                        cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpGet("personal-profiles/{personalProfileId:guid}")]
        public async Task<IActionResult> GetDetail(
            Guid personalProfileId,
            CancellationToken cancellationToken)
        {
            var result =
                await _moderatorService
                    .GetPersonalVerificationDetailAsync(
                        personalProfileId,
                        cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(result.Data);
        }


        [HttpPost("personal-profiles/{personalProfileId:guid}/review")]
        public async Task<IActionResult> ReviewPersonalProfile(Guid personalProfileId,
        [FromBody] ReviewPersonalIdentityRequest request,
        CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _moderatorService.ReviewPersonalIdentityAsync(moderatorId, personalProfileId,
                    request,
                    cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpPatch("posts/{postId:guid}/suspend")]
        public async Task<IActionResult> SuspendPost(Guid postId, CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _postService.SuspendAsync(postId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = "Bài đăng đã bị đình chỉ (Suspended). Bài đăng sẽ không còn hiển thị trên trang chủ người dùng."
            });
        }

        [HttpPost("withdrawals/{withdrawalId:guid}/approve")]
        public async Task<IActionResult> ApproveWithdrawal(
            [FromRoute] Guid withdrawalId, CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _withdrawalService.ApproveWithdrawalAsync(moderatorId, withdrawalId, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new { success = true, message = "Đã duyệt yêu cầu rút tiền, đang chuyển tiền." });
        }

        [HttpPost("withdrawals/{withdrawalId:guid}/reject")]
        public async Task<IActionResult> RejectWithdrawal(
            [FromRoute] Guid withdrawalId,
            [FromBody] RejectWithdrawalRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();
            if (moderatorId == Guid.Empty)
                return Unauthorized(new { success = false, message = "Phiên làm việc không hợp lệ." });

            var result = await _withdrawalService.RejectWithdrawalAsync(
                moderatorId, withdrawalId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    code = result.Error.Code,
                    message = result.Error.Message
                });
            }

            return Ok(new { success = true, message = "Đã từ chối yêu cầu rút tiền." });
        }


        [HttpGet("disputes")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tranh chấp cho Moderator",
            Description = "Trả về danh sách tranh chấp có hỗ trợ lọc, tìm kiếm và phân trang."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDisputes(
            [FromQuery] DisputeSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _disputeService.GetAllForModeratorAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }


        [HttpGet("disputes/{disputeId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết tranh chấp",
            Description = "Trả về thông tin chi tiết tranh chấp và các action hiện tại của Moderator."
        )]
        [ProducesResponseType(typeof(DisputeDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDisputeDetail(Guid disputeId, CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();

            if (moderatorId == Guid.Empty)
                return Unauthorized();

            var result = await _disputeService.GetDetailForModeratorAsync(
                disputeId,
                moderatorId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }


        [HttpPost("disputes/{disputeId:guid}/claim")]
        [SwaggerOperation(
            Summary = "Moderator tiếp nhận tranh chấp",
            Description = "Gán tranh chấp đang Pending cho Moderator hiện tại và chuyển trạng thái sang UnderReview."
        )]
        [ProducesResponseType(typeof(ClaimDisputeResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClaimDispute(
            Guid disputeId,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();

            if (moderatorId == Guid.Empty)
                return Unauthorized();

            var result = await _disputeService.ClaimForModeratorAsync(
                disputeId,
                moderatorId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }


        [HttpPost("disputes/{disputeId:guid}/resolve")]
        [SwaggerOperation(
            Summary = "Moderator xác nhận tranh chấp có căn cứ",
            Description = "Kết thúc quá trình đánh giá tranh chấp với trạng thái Resolved. Order tiếp tục ở trạng thái Disputing để chờ bước xử lý settlement."
        )]
        [ProducesResponseType(typeof(DisputeDecisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResolveDispute(
            Guid disputeId,
            [FromBody] DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();

            if (moderatorId == Guid.Empty)
                return Unauthorized();

            var result = await _disputeService.ResolveByModeratorAsync(
                disputeId,
                moderatorId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }


        [HttpPost("disputes/{disputeId:guid}/reject")]
        [SwaggerOperation(
            Summary = "Moderator từ chối tranh chấp",
            Description = "Kết thúc tranh chấp do không hợp lệ hoặc không đủ căn cứ và khôi phục trạng thái Order trước khi tranh chấp."
        )]
        [ProducesResponseType(typeof(DisputeDecisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectDispute(
            Guid disputeId,
            [FromBody] DisputeModeratorDecisionRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();

            if (moderatorId == Guid.Empty)
                return Unauthorized();

            var result = await _disputeService.RejectByModeratorAsync(
                disputeId,
                moderatorId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }

        private IActionResult MapDisputeError(Error error)
        {
            if (error == DisputeErrors.NotFound || error == OrderErrors.NotFound)
                return NotFound(error);

            if (error == DisputeErrors.Forbidden || error == DisputeErrors.NotAssignedModerator)
                return StatusCode(StatusCodes.Status403Forbidden, error);

            if (error == DisputeErrors.AlreadyClaimed ||
                error == DisputeErrors.ClaimNotAllowed ||
                error == DisputeErrors.DecisionNotAllowed ||
                error == OrderErrors.NotDisputing)
            {
                return Conflict(error);
            }

            return BadRequest(error);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
