using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Services.Offers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NegotiationsController : ControllerBase
    {
        private readonly IOfferService _offerService;

        public NegotiationsController(IOfferService offerService)
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

        private IActionResult MapErrorToResponse(Error error)
        {
            return error.Code switch
            {
                var code when code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => NotFound(error),
                var code when code.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) => Forbid(),
                _ => BadRequest(error)
            };
        }

        // ==================== READ ====================

        [HttpGet("{negotiationId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết phòng thương lượng",
            Description =
                "Lấy trạng thái, hai bên tham gia, giá cuối cùng và số lượng cuối cùng " +
                "của một Negotiation. Chỉ Buyer hoặc Seller của phiên được xem."
        )]
        public async Task<IActionResult> GetById(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationByIdAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not NegotiationResponse response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }


        [HttpGet("by-offer/{offerId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy phòng thương lượng theo offer",
            Description =
                "Tìm Negotiation được tạo từ một Offer. FE dùng endpoint này khi " +
                "người dùng chọn một offer đã được Accept hoặc Counter."
        )]
        public async Task<IActionResult> GetByOfferId(
            Guid offerId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationByOfferIdAsync(
                CurrentUserId,
                offerId,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not NegotiationResponse response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }

        [HttpGet("{negotiationId:guid}/messages")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử thương lượng",
            Description =
                "Lấy danh sách message và proposal của một Negotiation theo phân trang. " +
                "Chỉ hai người tham gia phiên thương lượng được xem."
        )]
        public async Task<IActionResult> GetMessages(
            Guid negotiationId,
            [FromQuery] PaginationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.GetNegotiationMessagesAsync(
                CurrentUserId,
                negotiationId,
                request,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not PagedResult<MessageResponse> response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }

        // ==================== NEGOTIATION ACTIONS ====================

        [HttpPost("{negotiationId:guid}/counter")]
        [SwaggerOperation(
            Summary = "Gửi counter trong phòng thương lượng",
            Description =
                "Buyer hoặc Seller gửi một proposal mới. Proposal Pending trước đó " +
                "của đối phương chuyển thành Superseded. Không được tự counter " +
                "proposal Pending do chính mình vừa gửi."
        )]
        public async Task<IActionResult> Counter(
            Guid negotiationId,
            [FromBody] SendNegotiationCounterRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.SendNegotiationCounterAsync(
                CurrentUserId,
                negotiationId,
                request,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not NegotiationResponse response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }

        [HttpPost("{negotiationId:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận proposal và chốt thương lượng",
            Description =
                "Chấp nhận proposal Pending của đối phương. Negotiation chuyển sang " +
                "Agreed, số lượng bài đăng được trừ và Agreement Form được tạo. " +
                "Không được chấp nhận proposal do chính mình gửi."
        )]
        public async Task<IActionResult> Accept(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.AcceptNegotiationAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not NegotiationResponse response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }

        [HttpPost("{negotiationId:guid}/reject-proposal")]
        [SwaggerOperation(
            Summary = "Từ chối proposal hiện tại",
            Description =
                "Từ chối proposal Pending của đối phương. Proposal chuyển sang " +
                "Rejected nhưng Negotiation vẫn Open để hai bên có thể tiếp tục counter."
        )]
        public async Task<IActionResult> RejectProposal(
            Guid negotiationId,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.RejectNegotiationProposalAsync(
                CurrentUserId,
                negotiationId,
                cancellationToken);

            if (!result.IsSuccess ||
                result.Data is not NegotiationResponse response)
            {
                return MapErrorToResponse(result.Error!);
            }

            return Ok(response);
        }

        //[HttpPost("{negotiationId:guid}/close")]
        //[SwaggerOperation(
        //    Summary = "Đóng phòng thương lượng",
        //    Description =
        //        "Một trong hai bên chủ động kết thúc Negotiation mà không chốt " +
        //        "thỏa thuận. Negotiation chuyển từ Open sang Closed và không thể " +
        //        "gửi hoặc xử lý proposal mới."
        //)]
        //public async Task<IActionResult> Close(
        //    Guid negotiationId,
        //    CancellationToken cancellationToken)
        //{
        //    var result = await _offerService.CloseNegotiationAsync(
        //        CurrentUserId,
        //        negotiationId,
        //        cancellationToken);

        //    if (!result.IsSuccess ||
        //        result.Data is not NegotiationResponse response)
        //    {
        //        return MapErrorToResponse(result.Error!);
        //    }

        //    return Ok(response);
        //}
    }
}
