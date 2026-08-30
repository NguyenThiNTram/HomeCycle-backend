using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/negotiations")]
    [ApiController]
    [Authorize]
    public class NegotiationsController : ControllerBase
    {
        private readonly INegotiationService _negotiationService;

        public NegotiationsController(INegotiationService negotiationService)
        {
            _negotiationService = negotiationService;
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

        // ==================== READ ====================

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách phiên thương lượng của tôi",
            Description =
                "Phân trang các Negotiation mà người dùng hiện tại tham gia với vai trò " +
                "Buyer hoặc Seller."
        )]
        [ProducesResponseType(typeof(Result<PagedResult<NegotiationListItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PagedResult<NegotiationListItemResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyNegotiations(
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.GetMyNegotiationsAsync(
                CurrentUserId,
                request,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("{negotiationId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết phòng thương lượng",
            Description =
                "Lấy trạng thái, hai bên tham gia, giá cuối cùng, số lượng cuối cùng " +
                "và toàn bộ proposal/message của một Negotiation. " +
                "Chỉ Buyer hoặc Seller của phiên được xem."
        )]
        [ProducesResponseType(typeof(Result<NegotiationDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationDetailResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.GetByIdAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("by-offer/{offerId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy phòng thương lượng theo offer",
            Description =
                "Tìm Negotiation được tạo từ một Offer. FE dùng endpoint này khi " +
                "người dùng chọn một offer đã được Accept hoặc Counter."
        )]
        [ProducesResponseType(typeof(Result<NegotiationDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationDetailResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByOfferId(
            Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.GetByOfferIdAsync(
                CurrentUserId,
                offerId,
                cancellationToken);

            return HandleResult(result);
        }

        // ==================== NEGOTIATION ACTIONS ====================

        [HttpPost("{negotiationId:guid}/counter")]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Gửi counter trong phòng thương lượng",
            Description =
                "Buyer hoặc Seller gửi một proposal mới. Proposal Pending trước đó " +
                "của đối phương chuyển thành Superseded. Không được tự counter " +
                "proposal Pending do chính mình vừa gửi."
        )]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Counter(
            Guid negotiationId,
            [FromBody] SendNegotiationCounterRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.CounterAsync(
                CurrentUserId,
                negotiationId,
                request,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("{negotiationId:guid}/proposals/{proposalMessageId:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận proposal và chốt thương lượng",
            Description =
                "Chỉ Buyer mới được chấp nhận proposal Pending của đối phương. " +
                "Negotiation chuyển sang Agreed và chốt FinalPrice/FinalQuantity. " +
                "Không được chấp nhận proposal do chính mình gửi."
        )]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptProposal(
            Guid negotiationId,
            Guid proposalMessageId,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.AcceptProposalAsync(
                CurrentUserId,
                negotiationId,
                proposalMessageId,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPatch("{negotiationId:guid}/proposals/{proposalMessageId:guid}/reject")]
        [SwaggerOperation(
            Summary = "Từ chối proposal hiện tại",
            Description =
                "Từ chối proposal Pending của đối phương. Proposal chuyển sang " +
                "Rejected nhưng Negotiation vẫn Open để hai bên có thể tiếp tục counter."
        )]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectProposal(
            Guid negotiationId,
            Guid proposalMessageId,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.RejectProposalAsync(
                CurrentUserId,
                negotiationId,
                proposalMessageId,
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("{negotiationId:guid}/cancel")]
        [SwaggerOperation(
            Summary = "Hủy phiên thương lượng",
            Description =
                "Một trong hai bên chủ động kết thúc Negotiation mà không chốt " +
                "thỏa thuận. Negotiation chuyển sang Closed và không thể gửi " +
                "hoặc xử lý proposal mới."
        )]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationActionResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _negotiationService.CancelAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            return HandleResult(result);
        }

        #region PRIVATE HELPERS

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        #endregion
    }
}
