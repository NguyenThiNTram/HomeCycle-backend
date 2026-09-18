using System.Security.Claims;
using FluentValidation;
using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.Interfaces.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/ai/price-suggestions")]
[Authorize(Roles = "Personal")]
public sealed class PriceSuggestionsController(
    IPriceSuggestionService suggestions,
    IPriceSuggestionQuota quota,
    IValidator<AiPriceDraftRequest> validator) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("quota")]
    public async Task<IActionResult> GetQuota(CancellationToken cancellationToken)
    {
        if (UserId is not Guid userId) return Unauthorized();
        var dailyLimit = await quota.GetDailyLimitAsync(userId, cancellationToken);
        var remaining = await quota.RemainingAsync(userId, dailyLimit, cancellationToken);
        return Ok(new { dailyLimit, remainingToday = remaining, resetsAt = quota.ResetsAt });
    }

    [HttpPost("draft")]
    public async Task<IActionResult> Suggest([FromBody] AiPriceDraftRequest draft,
        CancellationToken cancellationToken)
    {
        if (UserId is not Guid userId) return Unauthorized();
        var validation = await validator.ValidateAsync(draft, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { code = "INSUFFICIENT_INPUT",
                message = "Thông tin sản phẩm chưa đủ hoặc chưa hợp lệ để gợi ý giá.",
                errors = validation.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(x => x.Key, x => x.Select(error => error.ErrorMessage).Distinct().ToArray()) });
        var response = await suggestions.SuggestAsync(userId, draft, cancellationToken);
        if (response is null)
            return BadRequest(new { code = "INSUFFICIENT_INPUT",
                message = "Cần loại sản phẩm, thương hiệu, tên, model, tình trạng hoạt động và mức hư hại." });
        if (response.Status == "DAILY_LIMIT_REACHED") return StatusCode(StatusCodes.Status429TooManyRequests, response);
        return Ok(response);
    }
}
