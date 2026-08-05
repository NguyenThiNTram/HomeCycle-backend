using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
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
        [SwaggerOperation(
            Summary = "Tạo đề nghị thương lượng mới (Offer)",
            Description = "Người dùng gửi đề nghị giá và số lượng từ một bài đăng. Yêu cầu này ở trạng thái Pending và chưa kích hoạt phòng chat Negotiation."
        )]
        public async Task<IActionResult> CreateOffer(
            [FromBody] CreateOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CreateOfferAsync(CurrentUserId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{offerId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật đề nghị thương lượng ban đầu",
            Description = "Người gửi (Sender) chỉ được phép chỉnh sửa giá và số lượng mong muốn khi đề nghị đang ở trạng thái Pending."
        )]
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
        public async Task<IActionResult> RejectOffer(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.RejectAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{offerId:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận mở thương lượng",
            Description = "Người nhận (Receiver) chấp nhận request ban đầu. Hệ thống đổi OfferStatus sang Accepted và tạo phiên Negotiation mới (chưa chốt giao dịch hay trừ tồn kho)."
        )]
        public async Task<IActionResult> AcceptOffer(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.AcceptAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{offerId:guid}/counter")]
        [SwaggerOperation(
            Summary = "Phản đề nghị ban đầu (Counter Initial Offer)",
            Description = "Người nhận (Receiver) đưa ra mức giá/số lượng khác cho đề nghị ban đầu. Hệ thống tự động tạo phiên Negotiation, lưu proposal gốc thành Superseded và lưu mức counter mới ở trạng thái Pending."
        )]
        public async Task<IActionResult> CounterInitialOffer(
            [FromRoute] Guid offerId,
            [FromBody] CounterInitialOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CounterInitialOfferAsync(CurrentUserId, offerId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("negotiations/{negotiationId:guid}/counter")]
        [SwaggerOperation(
            Summary = "Gửi phản đề nghị trong phiên thương lượng",
            Description = "Một trong hai bên tham gia gửi đề nghị giá/số lượng mới trong Negotiation. Đề nghị Pending trước đó của đối phương sẽ chuyển thành Superseded."
        )]
        public async Task<IActionResult> SendNegotiationCounter(
            [FromRoute] Guid negotiationId,
            [FromBody] SendNegotiationCounterRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.SendNegotiationCounterAsync(CurrentUserId, negotiationId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("negotiations/{negotiationId:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận thương lượng trong phòng chat",
            Description = "Ràng buộc cứng: Chỉ Người mua (Buyer) mới có quyền gọi hành động chốt deal này. Trạng thái Negotiation chuyển thành Agreed và chốt FinalPrice/FinalQuantity."
        )]
        public async Task<IActionResult> AcceptNegotiation(
            [FromRoute] Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.AcceptNegotiationAsync(CurrentUserId, negotiationId, cancellationToken);
            return HandleResult(result);
        }
        [HttpPost("negotiations/{negotiationId:guid}/reject")]
        [SwaggerOperation(
            Summary = "Từ chối đề nghị trong phòng thương lượng",
            Description = "Từ chối đề nghị Pending của đối phương. Phiên thương lượng (Negotiation) vẫn ở trạng thái Open để hai bên tiếp tục trao đổi."
        )]
        public async Task<IActionResult> RejectNegotiationProposal(
            [FromRoute] Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.RejectNegotiationProposalAsync(CurrentUserId, negotiationId, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{offerId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin đề nghị theo ID",
            Description = "Trả về thông tin chi tiết của một Offer. Chỉ người gửi hoặc người nhận mới có quyền xem."
        )]
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
        public async Task<IActionResult> GetReceived(
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetReceivedAsync(CurrentUserId, request, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("negotiations/{negotiationId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin phiên thương lượng theo ID",
            Description = "Trả về thông tin chi tiết phiên Negotiation. Yêu cầu người dùng phải là Buyer hoặc Seller của phiên đó."
        )]
        public async Task<IActionResult> GetNegotiationById(
            [FromRoute] Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationByIdAsync(CurrentUserId, negotiationId, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{offerId:guid}/negotiation")]
        [SwaggerOperation(
            Summary = "Lấy thông tin thương lượng theo Offer ID",
            Description = "Tra cứu thông tin phiên Negotiation tương ứng dựa trên OfferId gốc."
        )]
        public async Task<IActionResult> GetNegotiationByOfferId(
            [FromRoute] Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationByOfferIdAsync(CurrentUserId, offerId, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("negotiations/{negotiationId:guid}/messages")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử tin nhắn trong phiên thương lượng",
            Description = "Lấy danh sách tin nhắn và lịch sử các mức giá đề nghị (phân trang) trong một phiên Negotiation."
        )]
        public async Task<IActionResult> GetNegotiationMessages(
            [FromRoute] Guid negotiationId,
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationMessagesAsync(CurrentUserId, negotiationId, request, cancellationToken);
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
