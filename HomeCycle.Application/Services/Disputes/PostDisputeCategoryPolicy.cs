using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public static class PostDisputeCategoryPolicy
{
    public static IReadOnlyList<DisputeCategory> BuildAllowedCategories() =>
        [DisputeCategory.MisleadingPost, DisputeCategory.ProhibitedItem,
         DisputeCategory.Spam, DisputeCategory.InappropriateContent,
         DisputeCategory.FraudOrScam, DisputeCategory.Other];

    public static bool IsAllowed(DisputeCategory category) => BuildAllowedCategories().Contains(category);

    public static bool IsAllowed(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return normalizedCode is
            "MISLEADING_POST" or
            "PROHIBITED_ITEM" or
            "SPAM" or
            "INAPPROPRIATE_CONTENT" or
            "FRAUD_OR_SCAM" or
            "OTHER";
    }
}
