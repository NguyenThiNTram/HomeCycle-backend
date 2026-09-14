using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public static class ReviewDisputeCategoryPolicy
{
    public static IReadOnlyList<DisputeCategory> BuildAllowedCategories() =>
        [DisputeCategory.AbusiveReview, DisputeCategory.Harassment,
         DisputeCategory.Spam, DisputeCategory.InappropriateContent, DisputeCategory.Other];

    public static bool IsAllowed(DisputeCategory category) => BuildAllowedCategories().Contains(category);

    public static bool IsAllowed(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return normalizedCode is
            "ABUSIVE_REVIEW" or
            "HARASSMENT" or
            "SPAM" or
            "INAPPROPRIATE_CONTENT" or
            "OTHER";
    }
}
