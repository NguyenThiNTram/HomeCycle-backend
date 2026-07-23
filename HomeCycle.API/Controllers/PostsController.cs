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
using System.Security.Claims;

namespace HomeCycle.API.Controllers
{
    [Route("api/posts")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IProductAttributeService _productAttributeService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("create/sell")]
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

        [HttpPut("update/sell/{id:guid}")]
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
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(PagedResult<PostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
        {
            var result = await _postService.GetAllAsync(request, cancellationToken);
            return Ok(result.Value);
        }

        [HttpPost("search")]
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

        [HttpGet("~/api/product-types/{productTypeId:guid}/filterable-attributes")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(IReadOnlyList<AttributeFilterOptionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFilterableAttributes(
             Guid productTypeId, CancellationToken cancellationToken)
        {
            var result = await _productAttributeService.GetFilterableAttributesAsync(productTypeId, cancellationToken);
            return Ok(result.Value);
        }

        [HttpPatch("{id:guid}/close")]
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
