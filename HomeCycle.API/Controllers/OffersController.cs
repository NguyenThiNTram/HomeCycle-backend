using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
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
            Summary = "Tạo offer",
            Description = "Người mua gửi đề nghị mua (offer) cho bài đăng bán. Tự động tạo phiên thương lượng (Negotiation Open) và tin nhắn CounterOffer đầu tiên trong cùng một transaction."
        )]
        public async Task<IActionResult> CreateOffer(
            [FromBody] CreateOfferRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _offerService.CreateOfferAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.OfferId },
                response
            );
        }

        [HttpGet("{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết offer",
            Description = "Xem thông tin chi tiết của một offer (chỉ người gửi hoặc người nhận)."
        )]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _offerService.GetByIdAsync(CurrentUserId, id, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return Ok(response);
        }

        [HttpGet("sent")]
        [SwaggerOperation(
            Summary = "Danh sách offer đã gửi",
            Description = "Danh sách offer mà người dùng hiện tại là người gửi."
        )]
        public async Task<IActionResult> GetSent([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _offerService.GetSentAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("received")]
        [SwaggerOperation(
            Summary = "Danh sách offer nhận được",
            Description = "Danh sách offer mà người dùng hiện tại là người nhận."
        )]
        public async Task<IActionResult> GetReceived([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _offerService.GetReceivedAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Data);
        }

        [HttpPut("{id:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật offer",
            Description = "Cập nhật giá/số lượng của offer khi đang ở trạng thái Pending (chỉ người gửi)."
        )]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfferRequest request, CancellationToken cancellationToken)
        {
            var result = await _offerService.UpdateAsync(CurrentUserId, id, request, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return Ok(response);
        }

        [HttpPost("{id:guid}/cancel")]
        [SwaggerOperation(
            Summary = "Hủy offer",
            Description = "Người gửi hủy offer (soft state) khi offer đang Pending. Phiên thương lượng được đóng."
        )]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var result = await _offerService.CancelAsync(CurrentUserId, id, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return Ok(response);
        }

        [HttpPost("{id:guid}/accept")]
        [SwaggerOperation(
            Summary = "Chấp nhận offer",
            Description = "Người nhận chấp nhận đề nghị. Trừ RemainingQuantity của bài đăng, đóng bài đăng nếu hết hàng, Negotiation chuyển sang Agreed."
        )]
        public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
        {
            var result = await _offerService.AcceptAsync(CurrentUserId, id, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return Ok(response);
        }

        [HttpPost("{id:guid}/reject")]
        [SwaggerOperation(
            Summary = "Từ chối offer",
            Description = "Người nhận từ chối đề nghị. Negotiation giữ trạng thái Open để có thể gửi counter-offer mới."
        )]
        public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
        {
            var result = await _offerService.RejectAsync(CurrentUserId, id, cancellationToken);

            if (!result.IsSuccess || result.Data is not OfferResponse response)
                return MapErrorToResponse(result.Error!);

            return Ok(response);
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
    }
}
