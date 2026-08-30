using HomeCycle.Application.DTOs.Requests.Inspections;
using HomeCycle.Application.Interfaces.Services.Inspections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/inspection-forms")]
    public class InspectionFormController : ControllerBase
    {
        private readonly IInspectionFormService _inspectionService;

        public InspectionFormController(IInspectionFormService inspectionService)
        {
            _inspectionService = inspectionService;
        }

        [HttpGet("appointment/{appointmentId:guid}")]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.GetByAppointmentAsync(appointmentId, GetCurrentUserId(), cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("appointment/{appointmentId:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDraft(Guid appointmentId, [FromForm] CreateInspectionFormRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.CreateDraftAsync(appointmentId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPut("{inspectionFormId:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDraft(Guid inspectionFormId, [FromForm] UpdateInspectionFormRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.UpdateDraftAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("{inspectionFormId:guid}/submit")]
        public async Task<IActionResult> Submit(Guid inspectionFormId, [FromBody] InspectionRevisionRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.SubmitAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("{inspectionFormId:guid}/confirm")]
        public async Task<IActionResult> SellerConfirm(Guid inspectionFormId, [FromBody] InspectionRevisionRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.SellerConfirmAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("{inspectionFormId:guid}/reject")]
        public async Task<IActionResult> SellerReject(Guid inspectionFormId, [FromBody] RejectInspectionFormRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.SellerRejectAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("{inspectionFormId:guid}/collect-now")]
        public async Task<IActionResult> CollectNow(Guid inspectionFormId, [FromBody] InspectionRevisionRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.CollectNowAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        [HttpPost("{inspectionFormId:guid}/cancel-transaction")]
        public async Task<IActionResult> CancelTransaction(Guid inspectionFormId, [FromBody] InspectionRevisionRequest request, CancellationToken cancellationToken)
        {
            var result = await _inspectionService.CancelTransactionAsync(inspectionFormId, GetCurrentUserId(), request, cancellationToken);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc thiếu thông tin định danh người dùng.");

            return userId;
        }
    }
}
