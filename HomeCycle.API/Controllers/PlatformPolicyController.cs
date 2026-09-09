using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using HomeCycle.Application.DTOs.Responses.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/admin/platform-policies")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PlatformPolicyController : ControllerBase
    {
        private readonly IPlatformPolicyService _platformPolicyService;

        public PlatformPolicyController(IPlatformPolicyService platformPolicyService)
        {
            _platformPolicyService = platformPolicyService;
        }

        [HttpGet("dispute")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình khiếu nại hiện hành",
            Description =
                "Dùng cho màn hình cấu hình của Admin. " +
                "Trả về phiên bản Dispute Policy đang active và các rule xử lý khiếu nại.")]
        public async Task<IActionResult> GetDisputePolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetDisputePolicyAsync(cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("dispute")]
        [SwaggerOperation(
            Summary = "Cập nhật cấu hình khiếu nại",
            Description =
                "Cho phép Admin cập nhật một hoặc nhiều rule của Dispute Policy. " +
                "Hệ thống giữ phiên bản cũ và tạo một phiên bản mới nếu dữ liệu thay đổi.")]
        public async Task<IActionResult> UpdateDisputePolicy(
            [FromBody] UpdateDisputePolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty) return Unauthorized();

            var result = await _platformPolicyService.UpdateDisputePolicyAsync(adminId, request, cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("appointment")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình lịch hẹn hiện hành",
            Description =
                "Dùng cho màn hình cấu hình của Admin. " +
                "Trả về phiên bản Appointment Policy đang active và các mốc thời gian áp dụng.")]
        public async Task<IActionResult> GetAppointmentPolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetAppointmentPolicyAsync(cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("appointment")]
        [SwaggerOperation(
            Summary = "Cập nhật cấu hình lịch hẹn",
            Description =
                "Cho phép Admin cập nhật một hoặc nhiều rule của Appointment Policy. " +
                "Nếu cấu hình thay đổi, hệ thống tạo phiên bản mới và giữ lại lịch sử cũ.")]
        public async Task<IActionResult> UpdateAppointmentPolicy(
            [FromBody] UpdateAppointmentPolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            if (adminId == Guid.Empty) return Unauthorized();

            var result = await _platformPolicyService.UpdateAppointmentPolicyAsync(adminId, request, cancellationToken);
            if (!result.IsSuccess) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy tất cả platform policy đang hoạt động",
            Description =
                "Dùng cho trang tổng quan cấu hình hệ thống của Admin. " +
                "Trả về thông tin tóm tắt của từng loại policy đang active.")]
        public async Task<IActionResult> GetAllActive(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetAllActiveAsync(cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{policyType}/versions")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử phiên bản của một policy",
            Description =
                "Dùng cho màn hình lịch sử cấu hình của Admin. " +
                "PolicyType hỗ trợ: Dispute, Appointment và FileUpload.")]
        public async Task<IActionResult> GetVersions(string policyType, CancellationToken cancellationToken)
        {
            if (!TryParsePolicyType(policyType, out var type))
                return BadRequest(Result<object>.Fail(
                    PlatformPolicyErrors.UnsupportedType(policyType)));

            var result = await _platformPolicyService.GetVersionsAsync(type, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{policyType}/versions/{version:int}")]
        [SwaggerOperation(
            Summary = "Xem chi tiết một phiên bản policy",
            Description =
                "Trả về nội dung JSON và metadata của một phiên bản cụ thể. " +
                "FE dùng để hiển thị chi tiết trước khi Admin quyết định khôi phục.")]
        public async Task<IActionResult> GetVersion(string policyType, int version, CancellationToken cancellationToken)
        {
            if (!TryParsePolicyType(policyType, out var type))
                return BadRequest(Result<object>.Fail(
                    PlatformPolicyErrors.UnsupportedType(policyType)));

            var result = await _platformPolicyService.GetVersionAsync(
                type,
                version,
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{policyType}/versions/{version:int}/restore")]
        [SwaggerOperation(
            Summary = "Khôi phục một phiên bản policy",
            Description =
                "Tạo một phiên bản active mới từ nội dung của phiên bản được chọn. " +
                "Không kích hoạt lại trực tiếp bản ghi cũ và không xóa lịch sử cấu hình.")]
        public async Task<IActionResult> RestoreVersion(string policyType, int version, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            if (adminId == Guid.Empty)
                return Unauthorized();

            if (!TryParsePolicyType(policyType, out var type))
                return BadRequest(Result<object>.Fail(
                    PlatformPolicyErrors.UnsupportedType(policyType)));

            var result = await _platformPolicyService.RestoreVersionAsync(
                adminId,
                type,
                version,
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("file-upload")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình upload file hiện hành",
            Description =
                "Dùng cho màn hình quản lý upload của Admin. " +
                "Trả về giới hạn kích thước và phần mở rộng được phép theo từng context.")]
        public async Task<IActionResult> GetFileUploadPolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetFileUploadPolicyAsync(cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("file-upload")]
        [SwaggerOperation(
            Summary = "Cập nhật rule upload file",
            Description =
                "Cho phép Admin cập nhật kích thước tối đa hoặc danh sách phần mở rộng " +
                "của một FileUploadContext. Nếu dữ liệu thay đổi, hệ thống tạo phiên bản mới.")]
        public async Task<IActionResult> UpdateFileUploadPolicy([FromBody] UpdateFileUploadPolicyRequest request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            if (adminId == Guid.Empty)
                return Unauthorized();

            var result = await _platformPolicyService.UpdateFileUploadPolicyAsync(adminId, request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("~/api/platform-policies/file-upload")]
        [SwaggerOperation(
            Summary = "Lấy rule upload file dành cho client",
            Description =
                "FE Web và Mobile gọi API này để lấy giới hạn kích thước và định dạng file " +
                "theo từng context trước khi chọn hoặc gửi file. Endpoint không yêu cầu đăng nhập " +
                "để hỗ trợ luồng đăng ký có avatar và giấy tờ định danh.")]
        public async Task<IActionResult> GetPublicFileUploadPolicy(CancellationToken cancellationToken)
        {
            var result = await _platformPolicyService.GetFileUploadPolicyAsync(cancellationToken);

            if (!result.IsSuccess || result.Data == null)
                return BadRequest(result);

            var response = new FileUploadPolicyClientResponseDto
            {
                Version = result.Data.Version,
                UpdatedAt = result.Data.UpdatedAt,
                Config = result.Data.Config
            };

            return Ok(Result<FileUploadPolicyClientResponseDto>.Success(response));
        }

        [HttpGet("payment")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình thanh toán hiện hành",
            Description =
                "Dùng cho màn hình cấu hình của Admin. " +
                "Trả về tỷ lệ đặt cọc và thời gian hết hạn thanh toán đang áp dụng.")]
        public async Task<IActionResult> GetPaymentPolicy(CancellationToken cancellationToken)
        {
            var result =
                await _platformPolicyService.GetPaymentPolicyAsync(
                    cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("payment")]
        [SwaggerOperation(
            Summary = "Cập nhật cấu hình thanh toán",
            Description =
                "Cho phép Admin cập nhật tỷ lệ đặt cọc hoặc thời gian hết hạn thanh toán. " +
                "Nếu cấu hình thay đổi, hệ thống tạo phiên bản mới và giữ lại lịch sử cũ.")]
        public async Task<IActionResult> UpdatePaymentPolicy(
            [FromBody] UpdatePaymentPolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            if (adminId == Guid.Empty)
                return Unauthorized();

            var result =
                await _platformPolicyService.UpdatePaymentPolicyAsync(
                    adminId,
                    request,
                    cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpGet("order")]
        [SwaggerOperation(
            Summary = "Lấy cấu hình vòng đời đơn hàng hiện hành",
            Description =
                "Dùng cho màn hình cấu hình của Admin. " +
                "Trả về thời gian Buyer được phép xác nhận nhận hàng sau khi Seller xác nhận bàn giao.")]
        public async Task<IActionResult> GetOrderPolicy(CancellationToken cancellationToken)
        {
            var result =
                await _platformPolicyService.GetOrderPolicyAsync(
                    cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPatch("order")]
        [SwaggerOperation(
            Summary = "Cập nhật cấu hình vòng đời đơn hàng",
            Description =
                "Cho phép Admin cập nhật thời gian Buyer xác nhận nhận hàng. " +
                "Nếu cấu hình thay đổi, hệ thống tạo phiên bản mới và giữ lại lịch sử cũ.")]
        public async Task<IActionResult> UpdateOrderPolicy(
            [FromBody] UpdateOrderPolicyRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            if (adminId == Guid.Empty)
                return Unauthorized();

            var result =
                await _platformPolicyService.UpdateOrderPolicyAsync(
                    adminId,
                    request,
                    cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private static bool TryParsePolicyType(string policyType, out PlatformPolicyType type)
        {
            type = default;

            if (string.IsNullOrWhiteSpace(policyType))
                return false;

            switch (policyType.Trim().ToLowerInvariant())
            {
                case "dispute":
                    type = PlatformPolicyType.Dispute;
                    return true;

                case "appointment":
                    type = PlatformPolicyType.Appointment;
                    return true;

                case "file-upload":
                case "fileupload":
                    type = PlatformPolicyType.FileUpload;
                    return true;

                case "payment":
                    type = PlatformPolicyType.Payment;
                    return true;

                case "order":
                    type = PlatformPolicyType.Order;
                    return true;

                default:
                    return false;
            }
        }
    }
}
