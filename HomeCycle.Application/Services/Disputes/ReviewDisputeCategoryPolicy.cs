using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public static class ReviewDisputeCategoryPolicy
{
    public static IReadOnlyList<DisputeCategory> BuildAllowedCategories() =>
        [DisputeCategory.AbusiveReview, DisputeCategory.Harassment,
         DisputeCategory.Spam, DisputeCategory.InappropriateContent, DisputeCategory.Other];

    public static bool IsAllowed(DisputeCategory category) => BuildAllowedCategories().Contains(category);
}
