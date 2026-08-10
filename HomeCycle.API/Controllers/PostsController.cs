using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/posts")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        //private Guid CurrentUserId =>
        //    Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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

        [HttpPost("create/sell")]
        [SwaggerOperation(
            Summary = "Tạo bài đăng bán",
            Description = "Tạo mới bài đăng bán sản phẩm với thông tin chi tiết và hình ảnh."
        )]
        //[Authorize(Roles = "Personal")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateSellPost(
            [FromForm] CreateSellPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.CreateSellPostAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess || result.Data is not PostResponse response)
                return BadRequest(result.Error);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.PostId },
                response
            );
        }

        [HttpPost("create/buy")]
        [SwaggerOperation(
            Summary = "Tạo bài đăng mua",
            Description = "Tạo mới bài đăng thu mua sản phẩm với thông tin chi tiết và hình ảnh."
        )]
        //[Authorize(Roles = "Business")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBuyPost(
            [FromForm] CreateBuyPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.CreateBuyPostAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess || result.Data is not PostResponse response)
                return BadRequest(result.Error);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.PostId },
                response
            );
        }

        [HttpPatch("update/sell/{postId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật bài đăng bán",
            Description = "Cập nhật thông tin bài đăng bán sản phẩm."
        )]
        //[Authorize(Roles = "Personal,Business")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateSellPost(
            Guid postId,
            [FromForm] UpdateSellPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.UpdateSellPostAsync(CurrentUserId, postId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Data);
        }

        [HttpPatch("update/buy/{postId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật bài đăng mua",
            Description = "Cập nhật thông tin bài đăng thu mua sản phẩm."
        )]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBuyPost(
            Guid postId,
            [FromForm] UpdateBuyPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.UpdateBuyPostAsync(CurrentUserId, postId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Data);
        }

        [HttpGet("get-by-id/{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết bài đăng",
            Description = "Trả về chi tiết thông tin của một bài đăng theo ID."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _postService.GetDetailAsync(id, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet("get-all")]
        [SwaggerOperation(
            Summary = "Lấy tất cả bài đăng (dành cho Moderator/Admin quản lý hệ thống)",
            Description = "Trả về danh sách TẤT CẢ bài đăng bất kể trạng thái (Active, Suspended, Closed, Deleted) có hỗ trợ phân trang. Chỉ dành cho Moderator/Admin."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _postService.GetAllAsync(request, cancellationToken);
            return Ok(result.Data);
        }

        [HttpGet("get-all-active")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài đăng hoạt động (trang chủ người dùng)",
            Description = "Trả về danh sách các bài đăng đang hoạt động (Active) cho trang chủ người dùng. Các bài bị đình chỉ (Suspended), đóng (Closed) hoặc xóa (Deleted) sẽ không xuất hiện."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _postService.GetAllActiveAsync(request, cancellationToken);
            return Ok(result.Data);
        }

        [HttpGet("get-all/by-user/{userId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài đăng của người dùng",
            Description = "Trả về danh sách bài đăng theo UserId có hỗ trợ phân trang."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllByUser(Guid userId, [FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _postService.GetAllByOwnerAsync(userId, request, cancellationToken);
            return Ok(result.Data);
        }

        [HttpGet("get-detail-by-user/{userId:guid}/{postId:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết bài đăng của người dùng",
            Description = "Trả về chi tiết bài đăng theo userId và postId."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetailByUser(Guid userId, Guid postId, CancellationToken cancellationToken)
        {
            var result = await _postService.GetDetailByOwnerAsync(userId, postId, cancellationToken);

            if (!result.IsSuccess)
                return NotFound(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm bài đăng",
            Description = "Tìm kiếm bài đăng theo nhiều tiêu chí với phân trang."
        )]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromBody] PostSearchRequest request,
                    CancellationToken cancellationToken)
        {
            var result = await _postService.SearchAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPatch("{postId:guid}/close")]
        [SwaggerOperation(
            Summary = "Đóng bài đăng",
            Description = "Đóng bài đăng (kết thúc giao dịch) của người dùng hiện tại."
        )]
        //[Authorize(Roles = "Personal,Business")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Close(Guid postId, CancellationToken cancellationToken)
        {
            var result = await _postService.CloseAsync(CurrentUserId, postId, cancellationToken);
            // TẠM THỜI — xóa sau khi xác nhận
            Console.WriteLine($"IsSuccess={result.IsSuccess}, Value is null={result.Data == null}");
            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return NoContent();
        }

        [HttpPatch("{postId:guid}/reactivate")]
        [SwaggerOperation(
            Summary = "Kích hoạt lại bài đăng",
            Description = "Kích hoạt lại bài đăng đã bị đóng (Closed) của người dùng hiện tại."
        )]
        public async Task<IActionResult> Reactivate(Guid postId, CancellationToken cancellationToken)
        {
            var result = await _postService.ReactivateAsync(CurrentUserId, postId, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return NoContent();
        }

        [HttpDelete("delete/{id:guid}")]
        [SwaggerOperation(
            Summary = "Xóa bài đăng",
            Description = "Admin xóa bài đăng của người dùng hiện tại khỏi hệ thống."
        )]
        [Authorize(Roles = "Admin")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _postService.DeleteAsync(CurrentUserId, id, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return NoContent();
        }

        private IActionResult MapErrorToResponse(Error error)
        {
            return error.Code switch
            {
                nameof(PostErrors.Forbidden) => Forbid(),
                nameof(PostErrors.NotFound) => NotFound(error),
                _ => BadRequest(error)
            };
        }
    }
}
