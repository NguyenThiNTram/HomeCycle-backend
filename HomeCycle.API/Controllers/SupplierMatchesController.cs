using System.Security.Claims;
using FluentValidation;
using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/ai/supplier-matches")]
[Authorize(Roles = "Business")]
public sealed class SupplierMatchesController(
    ISupplierMatchService matching,
    IPostService posts,
    IValidator<SupplierMatchDraftRequest> validator) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost("draft")]
    public async Task<IActionResult> MatchDraft(
        [FromBody] SupplierMatchDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (UserId is not Guid userId)
            return Unauthorized();

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new
            {
                code = "INSUFFICIENT_INPUT",
                message = "Thông tin nhu cầu mua chưa đủ hoặc chưa hợp lệ để tìm nhà cung cấp.",
                errors = validation.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).Distinct().ToArray())
            });

        return Ok(await matching.MatchDraftAsync(userId, request, cancellationToken));
    }

    [HttpPost("buy-post/{buyPostId:guid}")]
    public async Task<IActionResult> MatchBuyPost(
        Guid buyPostId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        SupplierMatchAdvancedFilterRequest? advancedFilters,
        CancellationToken cancellationToken)
    {
        if (UserId is not Guid userId) return Unauthorized();
        if (advancedFilters?.MinimumSellerRating is < 0 or > 5)
            return BadRequest(new
            {
                code = "INVALID_ADVANCED_FILTERS",
                message = "Điểm đánh giá nhà cung cấp phải nằm trong khoảng từ 0 đến 5."
            });
        var result = await posts.GetSupplierMatchesAsync(
            userId, buyPostId, advancedFilters, cancellationToken);
        if (result.IsSuccess) return Ok(result.Data);
        return result.Error?.Code switch
        {
            "POST_FORBIDDEN" => Forbid(),
            "POST_NOT_FOUND" => NotFound(result.Error),
            _ => BadRequest(result.Error)
        };
    }
}
