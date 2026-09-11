using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Requests.Moderators;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.DTOs.Responses.Inspections;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Inspections;
using HomeCycle.Application.Interfaces.Services.Moderators;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Payments;
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
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IAppointmentService _appointmentService;
        private readonly IInspectionFormService _inspectionFormService;
        public ModeratorController(IModeratorService moderatorService, IPostService postService, IWithdrawalService withdrawalService, IDisputeService disputeService, IPaymentService paymentService,
            IAppointmentService appointmentService, IOrderService orderService, IInspectionFormService inspectionFormService)
        {
            _moderatorService = moderatorService;
            _postService = postService;
            _withdrawalService = withdrawalService;
            _disputeService = disputeService;
            _paymentService = paymentService;
            _orderService = orderService;
            _appointmentService = appointmentService;
            _inspectionFormService = inspectionFormService;
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
            Summary = "Moderator giải quyết tranh chấp",
            Description = "Kết luận BuyerFavored hoặc SellerFavored và thực hiện xử lý nền tảng tương ứng."
        )]
        [ProducesResponseType(typeof(DisputeDecisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResolveDispute(
            Guid disputeId,
            [FromBody] ResolveDisputeRequest request,
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


        [HttpPost("disputes/{disputeId:guid}/verify-return")]
        [SwaggerOperation(
            Summary = "Moderator xác minh hoàn trả hàng",
            Description = "Xác minh việc hoàn trả sau khi Buyer đã xác nhận trả hàng và Seller không phản hồi đúng hạn."
        )]
        [ProducesResponseType(typeof(DisputeDecisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyDisputeReturn(
            Guid disputeId,
            [FromBody] VerifyDisputeReturnRequest request,
            CancellationToken cancellationToken)
        {
            var moderatorId = GetCurrentUserId();

            if (moderatorId == Guid.Empty)
                return Unauthorized();

            var result = await _disputeService.VerifyReturnByModeratorAsync(
                disputeId,
                moderatorId,
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return MapDisputeError(result.Error!);

            return Ok(result.Data);
        }


        [HttpGet("orders")]
        [SwaggerOperation(
              Summary = "Lấy danh sách đơn hàng cho Moderator",
              Description = "Trả về danh sách Order toàn hệ thống có hỗ trợ tìm kiếm, lọc trạng thái, Buyer, Seller, tranh chấp, kiểm định, thời gian tạo và phân trang."
          )]
        [ProducesResponseType(typeof(PagedResult<ModeratorOrderListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrders(
          [FromQuery] ModeratorOrderSearchRequest request,
          CancellationToken cancellationToken)
        {
            var result = await _orderService.GetAllForModeratorAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("orders/{orderId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết đơn hàng cho Moderator",
            Description = "Trả về thông tin Order theo góc nhìn trung lập của Moderator, gồm Buyer, Seller, thanh toán, vận chuyển, lịch hẹn, tranh chấp, hoàn trả và timeline nghiệp vụ."
        )]
        [ProducesResponseType(typeof(ModeratorOrderDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderDetail(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.GetDetailForModeratorAsync(orderId, cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("orders/{orderId:guid}/financial-history")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử tài chính của đơn hàng",
            Description = "Trả về các WalletTransaction liên quan trực tiếp đến Order như thanh toán, hoàn tiền, giải ngân và các biến động tài chính nghiệp vụ khác để Moderator kiểm tra dòng tiền."
        )]
        [ProducesResponseType(typeof(IReadOnlyList<OrderFinancialEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderFinancialHistory(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var result = await _paymentService.GetOrderFinancialHistoryForModeratorAsync(
                orderId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("appointments")]
        [SwaggerOperation(
            Summary = "Lấy danh sách lịch hẹn cho Moderator",
            Description = "Trả về danh sách Inspection và Collection Appointment toàn hệ thống, hỗ trợ tìm kiếm, lọc loại lịch, trạng thái, Order, Buyer, Seller, lịch quá hạn, Inspection Form, thời gian hẹn và phân trang."
        )]
        [ProducesResponseType(typeof(PagedResult<ModeratorAppointmentListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] ModeratorAppointmentSearchRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetAllForModeratorAsync(
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("appointments/{appointmentId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết lịch hẹn cho Moderator",
            Description = "Trả về thông tin chi tiết Appointment theo góc nhìn Moderator, gồm Order liên quan, Buyer, Seller, lịch hẹn, check-in, quá hạn, hủy lịch, reschedule và thông tin Inspection hoặc Collection."
        )]
        [ProducesResponseType(typeof(ModeratorAppointmentDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppointmentDetail(
            Guid appointmentId,
            CancellationToken cancellationToken)
        {
            var result = await _appointmentService.GetDetailForModeratorAsync(
                appointmentId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("appointments/{appointmentId:guid}/inspection-form")]
        [SwaggerOperation(
            Summary = "Lấy Inspection Form của lịch hẹn cho Moderator",
            Description = "Trả về biểu mẫu kiểm định và evidence của Inspection Appointment để Moderator kiểm tra kết quả kiểm định. API chỉ phục vụ đọc dữ liệu và không cấp action của Buyer hoặc Seller."
        )]
        [ProducesResponseType(typeof(InspectionFormResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppointmentInspectionForm(
            Guid appointmentId,
            CancellationToken cancellationToken)
        {
            var result = await _inspectionFormService.GetByAppointmentForModeratorAsync(
                appointmentId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("withdrawals")]
        [SwaggerOperation(
           Summary = "Lấy danh sách yêu cầu rút tiền cho Moderator",
           Description = "Trả về danh sách Withdrawal toàn hệ thống có hỗ trợ tìm kiếm, lọc User, trạng thái, thời gian yêu cầu và phân trang. Các yêu cầu cần xử lý được ưu tiên trong kết quả."
        )]
        [ProducesResponseType(typeof(PagedResult<ModeratorWithdrawalListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWithdrawals(
           [FromQuery] ModeratorWithdrawalSearchRequest request,
           CancellationToken cancellationToken)
        {
            var result = await _withdrawalService.GetAllForModeratorAsync(
                request,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("withdrawals/{withdrawalId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết yêu cầu rút tiền cho Moderator",
            Description = "Trả về thông tin chi tiết Withdrawal gồm User, tài khoản ngân hàng, trạng thái xử lý, Moderator xử lý, lý do từ chối và các financial event liên quan."
        )]
        [ProducesResponseType(typeof(ModeratorWithdrawalDetailDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWithdrawalDetail(
            Guid withdrawalId,
            CancellationToken cancellationToken)
        {
            var result = await _withdrawalService.GetDetailForModeratorAsync(
                withdrawalId,
                cancellationToken);

            if (!result.IsSuccess)
                return MapModeratorReadError(result.Error!);

            return Ok(result.Data);
        }

        private IActionResult MapModeratorReadError(Error error)
        {
            if (error == OrderErrors.NotFound ||
                error == AgreementErrors.NotFound ||
                error == AppointmentErrors.NotFound ||
                error == AppointmentErrors.InspectionDetailNotFound ||
                error == InspectionErrors.NotFound ||
                error.Code == "Withdrawal.NotFound")
            {
                return NotFound(error);
            }

            return BadRequest(error);
        }

        private IActionResult MapDisputeError(Error error)
        {
            if (error == DisputeErrors.NotFound ||
                error == OrderErrors.NotFound ||
                error == AgreementErrors.NotFound ||
                error == ProfileErrors.UserNotFound ||
                error == ProfileErrors.ProfileNotFound)
            {
                return NotFound(error);
            }

            if (error == DisputeErrors.Forbidden ||
                error == DisputeErrors.NotAssignedModerator)
            {
                return StatusCode(StatusCodes.Status403Forbidden, error);
            }

            if (error == DisputeErrors.AlreadyClaimed ||
                error == DisputeErrors.ClaimNotAllowed ||
                error == DisputeErrors.DecisionNotAllowed ||
                error == DisputeErrors.ReturnVerificationNotAllowed ||
                error.Code == "DISPUTE_RETURN_VERIFICATION_NOT_DUE" ||
                error == OrderErrors.NotDisputing ||
                error == OrderErrors.InvalidCompletionState)
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
