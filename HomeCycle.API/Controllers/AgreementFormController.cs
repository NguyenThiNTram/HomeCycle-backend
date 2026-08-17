using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Services.Agreements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetPreviewAsync(negotiationId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.CreateAgreementAsync(request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { AgreementId = result.Data });
        }

        [HttpGet("{agreementId}")]
        public async Task<IActionResult> GetDetail(Guid agreementId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetDetailAsync(agreementId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPut("{agreementId}")]
        public async Task<IActionResult> UpdateAgreement(Guid agreementId, [FromBody] UpdateAgreementFormRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.UpdateAgreementAsync(agreementId, request, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPatch("{id}/accept")]
        public async Task<IActionResult> AcceptAgreement(Guid id, [FromBody] AcceptAgreementRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.AcceptAgreementAsync(id, currentUserId, request.ExpectedRevision, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPatch("{id}/request-edit")]
        public async Task<IActionResult> RequestEdit(Guid id, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.RequestEditAsync(id, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("negotiations/{negotiationId:guid}/shipping-fee-preview")]
        [SwaggerOperation(
            Summary = "Xem trước phí vận chuyển",
            Description = "Trả về thông tin chi tiết về phí vận chuyển cho một cuộc đàm phán cụ thể dựa trên yêu cầu của người dùng"
        )]
        public async Task<IActionResult> PreviewShippingFee([FromRoute] Guid negotiationId, [FromBody] CalculateGhnFeeRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.PreviewShippingFeeAsync(
                negotiationId,
                currentUserId,
                request,
                cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Data);

            var error = result.Error!;

            return error.Code switch
            {
                "ShippingFee.InvalidRequest"
                    or "Ghn.InvalidFeeRequest"
                    or "Ghn.AddressRequired"
                    or "Ghn.InvalidServiceType"
                    or "Ghn.ParcelInformationRequired"
                    or "Ghn.ParcelInformationInvalid"
                    or "Ghn.HeavyItemsRequired"
                    or "Ghn.TotalWeightInvalid"
                        => BadRequest(error),

                "Auth.Forbidden" => StatusCode(StatusCodes.Status403Forbidden, error),
                "Negotiation.NotFound"
                    or "Product.NotFound"
                        => NotFound(error),

                "Negotiation.Cancelled" => Conflict(error),
                "Ghn.CalculateFeeFailed" => StatusCode(StatusCodes.Status502BadGateway, error),

                _ => BadRequest(error)
            };
        }

        [HttpGet("pending-payment")]
        public async Task<IActionResult> GetPendingPayment([FromQuery] PendingAgreementSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetPendingPaymentAsync(currentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("negotiations/{negotiationId:guid}/ghn-parcel-info")]
        [SwaggerOperation(
            Summary = "Lấy thông tin gói hàng GHN",
            Description = "Trả về thông tin chi tiết về gói hàng GHN cho một cuộc đàm phán cụ thể"
        )]
        public async Task<IActionResult> GetGhnParcelInfo([FromRoute] Guid negotiationId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.GetGhnParcelInfoAsync(negotiationId, currentUserId, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Data);

            var error = result.Error!;

            return error.Code switch
            {
                "Auth.Forbidden" => StatusCode(StatusCodes.Status403Forbidden, error),
                "Negotiation.NotFound"
                    or "Product.NotFound"
                        => NotFound(error),
                "Negotiation.Cancelled" => Conflict(error),
                _ => BadRequest(error)
            };
        }

        [HttpPost("negotiations/{negotiationId:guid}/ghn-preview")]
        [SwaggerOperation(
            Summary = "Xem trước thông tin vận chuyển GHN",
            Description = "Trả về thông tin chi tiết về phí vận chuyển GHN cho một cuộc đàm phán cụ thể dựa trên yêu cầu của người dùng"
        )]
        public async Task<IActionResult> PreviewGhnShipping([FromRoute] Guid negotiationId, [FromBody] GhnShippingPreviewRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _agreementService.PreviewGhnShippingAsync(negotiationId, currentUserId, request, cancellationToken);

            if (result.IsSuccess)
                return Ok(result.Data);

            var error = result.Error!;

            return error.Code switch
            {
                "ShippingFee.InvalidRequest"
                    or "Ghn.ParcelInformationRequired"
                        => BadRequest(error),
                "Auth.Forbidden" => StatusCode(StatusCodes.Status403Forbidden, error),
                "Negotiation.NotFound"
                    or "Product.NotFound"
                        => NotFound(error),
                "Negotiation.Cancelled" => Conflict(error),
                "Ghn.PreviewFailed" => StatusCode(StatusCodes.Status502BadGateway, error),
                _ => BadRequest(error)
            };
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
