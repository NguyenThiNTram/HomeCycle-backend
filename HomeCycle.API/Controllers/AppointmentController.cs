using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.Interfaces.Services.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/appointments")]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet("buyer/inspections")]
        public async Task<IActionResult> GetMyInspectionsAsBuyer(
            [FromQuery] AppointmentSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.GetInspectionListAsync(currentUserId, isSeller: false, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("seller/inspections")]
        public async Task<IActionResult> GetMyInspectionsAsSeller(
            [FromQuery] AppointmentSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.GetInspectionListAsync(currentUserId, isSeller: true, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("buyer/collections")]
        public async Task<IActionResult> GetMyCollectionsAsBuyer(
            [FromQuery] AppointmentSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.GetCollectionListAsync(currentUserId, isSeller: false, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpGet("seller/collections")]
        public async Task<IActionResult> GetMyCollectionsAsSeller(
            [FromQuery] AppointmentSearchRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.GetCollectionListAsync(currentUserId, isSeller: true, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        // ---- Chi tiết ----

        [HttpGet("{appointmentId:guid}")]
        public async Task<IActionResult> GetDetail(Guid appointmentId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.GetDetailAsync(appointmentId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        // ---- Check-in tại điểm hẹn (Phương án A) ----

        [HttpPost("{appointmentId:guid}/check-in")]
        public async Task<IActionResult> CheckIn(Guid appointmentId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _appointmentService.CheckInAsync(appointmentId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("{appointmentId:guid}/reschedule")]
        public async Task<IActionResult> RequestReschedule(Guid appointmentId, [FromBody] RescheduleAppointmentRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result =
                await _appointmentService.RequestRescheduleAsync(appointmentId, currentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }


        [HttpPost("{proposalAppointmentId:guid}/reschedule/accept")]
        public async Task<IActionResult> AcceptReschedule(Guid proposalAppointmentId, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _appointmentService.AcceptRescheduleAsync(proposalAppointmentId, currentUserId, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("{proposalAppointmentId:guid}/reschedule/reject")]
        public async Task<IActionResult> RejectReschedule(Guid proposalAppointmentId, [FromBody] RejectAppointmentRescheduleRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _appointmentService.RejectRescheduleAsync(proposalAppointmentId, currentUserId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }


        [HttpPost("{appointmentId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid appointmentId, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _appointmentService.CancelAsync(appointmentId, currentUserId, request, cancellationToken);

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
