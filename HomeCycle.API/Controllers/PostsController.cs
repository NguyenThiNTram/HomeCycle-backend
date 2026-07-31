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
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("create/sell")]
        [SwaggerOperation(
            Summary = "Tạo bài đăng bán",
            Description = "Tạo mới bài đăng bán sản phẩm với thông tin chi tiết và hình ảnh."
        )]
        //[Authorize(Roles = "Personal")]
        //[ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateSellPost(
            [FromForm] CreateSellPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.CreateSellPostAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess || result.Value is not PostResponse response)
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
        //[ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBuyPost(
            [FromForm] CreateBuyPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.CreateBuyPostAsync(CurrentUserId, request, cancellationToken);

            if (!result.IsSuccess || result.Value is not PostResponse response)
                return BadRequest(result.Error);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.PostId },
                response
            );
        }

        [HttpPut("update/sell/{postId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật bài đăng bán",
            Description = "Cập nhật thông tin bài đăng bán sản phẩm."
        )]
        //[Authorize(Roles = "Personal,Business")]
        //[ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateSellPost(
            Guid postId,
            [FromForm] UpdateSellPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.UpdateSellPostAsync(CurrentUserId, postId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Value);
        }

        [HttpPut("update/buy/{postId:guid}")]
        [SwaggerOperation(
            Summary = "Cập nhật bài đăng mua",
            Description = "Cập nhật thông tin bài đăng thu mua sản phẩm."
        )]
        [Consumes("multipart/form-data")]
        //[ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBuyPost(
            Guid postId,
            [FromForm] UpdateBuyPostRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _postService.UpdateBuyPostAsync(CurrentUserId, postId, request, cancellationToken);

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return Ok(result.Value);
        }

        [HttpGet("get-by-id/{id:guid}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết bài đăng",
            Description = "Trả về chi tiết thông tin của một bài đăng theo ID."
        )]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(PostDetailResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _postService.GetDetailAsync(id, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }

        [HttpGet("get-all")]
        [SwaggerOperation(
            Summary = "Lấy danh sách bài đăng",
            Description = "Trả về danh sách tất cả bài đăng trong hệ thống có hỗ trợ phân trang."
        )]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(PagedResult<PostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _postService.GetAllAsync(request, cancellationToken);
            return Ok(result.Value);
        }

        [HttpPost("search")]
        [SwaggerOperation(
            Summary = "Tìm kiếm bài đăng",
            Description = "Tìm kiếm bài đăng theo nhiều tiêu chí với phân trang."
        )]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(PagedResult<PostResponse>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromBody] PostSearchRequest request,
                    CancellationToken cancellationToken)
        {
            var result = await _postService.SearchAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Value);
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

            if (!result.IsSuccess)
                return MapErrorToResponse(result.Error!);

            return NoContent();
        }

        [HttpDelete("delete/{id:guid}")]
        [SwaggerOperation(
            Summary = "Xóa bài đăng",
            Description = "Xóa bài đăng của người dùng hiện tại khỏi hệ thống."
        )]
        //[Authorize(Roles = "Personal,Business")]
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
