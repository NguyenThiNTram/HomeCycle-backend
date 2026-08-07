using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Services.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/offers")]
    [ApiController]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly IOfferService _offerService;

        public OffersController(IOfferService offerService)
        {
            _offerService = offerService;
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

        [HttpPost]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Tạo đề nghị thương lượng mới (Offer)",
            Description = "Người dùng gửi đề nghị giá và số lượng từ một bài đăng. Yêu cầu này ở trạng thái Pending và chưa kích hoạt phòng chat Negotiation."
        )]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOffer(
            [FromBody] CreateOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CreateAsync(CurrentUserId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{offerId:guid}")]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Cập nhật đề nghị thương lượng ban đầu",
            Description = "Người gửi (Sender) chỉ được phép chỉnh sửa giá và số lượng mong muốn khi đề nghị đang ở trạng thái Pending."
        )]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOffer(
            [FromRoute] Guid offerId,
            [FromBody] UpdateOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.UpdateAsync(CurrentUserId, offerId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{offerId:guid}/cancel")]
        [SwaggerOperation(
            Summary = "Hủy đề nghị thương lượng ban đầu",
            Description = "Người gửi (Sender) tự hủy đề nghị của chính mình khi đề nghị đó vẫn đang ở trạng thái Pending."
        )]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelOffer(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CancelAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{offerId:guid}/reject")]
        [SwaggerOperation(
            Summary = "Từ chối đề nghị thương lượng ban đầu",
            Description = "Người nhận (Receiver) từ chối yêu cầu thương lượng ban đầu. Đề nghị chuyển sang trạng thái Rejected."
        )]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<OfferResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectOffer(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.RejectAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPatch("{offerId:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận mở thương lượng",
            Description = "Người nhận (Receiver) chấp nhận request ban đầu. Hệ thống đổi OfferStatus sang Accepted và tạo phiên Negotiation mới (chưa chốt giao dịch hay trừ tồn kho)."
        )]
        [ProducesResponseType(typeof(Result<AcceptOfferResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AcceptOfferResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AcceptOffer(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.AcceptAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPatch("{offerId:guid}/counter")]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Phản đề nghị ban đầu (Counter Initial Offer)",
            Description = "Người nhận (Receiver) đưa ra mức giá/số lượng khác cho đề nghị ban đầu. Hệ thống tự động tạo phiên Negotiation, lưu proposal gốc thành Superseded và lưu mức counter mới ở trạng thái Pending."
        )]
        [ProducesResponseType(typeof(Result<NegotiationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NegotiationResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CounterInitialOffer(
            [FromRoute] Guid offerId,
            [FromBody] CounterInitialOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CounterInitialOfferAsync(CurrentUserId, offerId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{offerId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin đề nghị theo ID",
            Description = "Trả về thông tin chi tiết của một Offer. Chỉ người gửi hoặc người nhận mới có quyền xem."
        )]
        [ProducesResponseType(typeof(Result<OfferDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<OfferDetailResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetByIdAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("sent")]
        [SwaggerOperation(
            Summary = "Lấy danh sách đề nghị đã gửi",
            Description = "Phân trang danh sách các Offer do người dùng hiện tại chủ động gửi đi."
        )]
        [ProducesResponseType(typeof(Result<PagedResult<OfferListItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PagedResult<OfferListItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSent(
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetSentAsync(CurrentUserId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("received")]
        [SwaggerOperation(
            Summary = "Lấy danh sách đề nghị đã nhận",
            Description = "Phân trang danh sách các Offer do người dùng hiện tại nhận được từ đối tác khác."
        )]
        [ProducesResponseType(typeof(Result<PagedResult<OfferListItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PagedResult<OfferListItem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReceived(
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetReceivedAsync(CurrentUserId, request, cancellationToken);
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
