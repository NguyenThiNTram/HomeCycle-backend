using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public static class PostDisputeCategoryPolicy
{
    public static IReadOnlyList<DisputeCategory> BuildAllowedCategories() =>
        [DisputeCategory.MisleadingPost, DisputeCategory.ProhibitedItem,
         DisputeCategory.Spam, DisputeCategory.InappropriateContent,
         DisputeCategory.FraudOrScam, DisputeCategory.Other];

    public static bool IsAllowed(DisputeCategory category) => BuildAllowedCategories().Contains(category);
}
