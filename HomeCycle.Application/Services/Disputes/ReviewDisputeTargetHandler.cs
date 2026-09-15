using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public sealed class ReviewDisputeTargetHandler(
    IReviewRepository reviews, IDisputeRepository disputes, IMediaService media) : IDisputeTargetHandler
{
    public DisputeTargetType TargetType => DisputeTargetType.Review;

    public async Task<Result<DisputeTargetCreateContext>> PrepareCreateAsync(
        Guid senderId, Guid targetId, string categoryCode, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var review = await reviews.GetByIdForUpdateAsync(targetId, cancellationToken);
        if (review == null)
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.ReviewNotFound);
        if (review.ReviewerId == senderId)
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.SelfReportNotAllowed);
        if (review.ReviewStatus is not ((int)ReviewStatus.Active or (int)ReviewStatus.Edited))
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.TargetUnavailable);
        if (!ReviewDisputeCategoryPolicy.IsAllowed(categoryCode))
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.InvalidCategory);
        if (await disputes.HasOpenDuplicateAsync(senderId, TargetType, targetId, cancellationToken))
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.DuplicateOpenReport);

        return Result<DisputeTargetCreateContext>.Success(new()
        {
            TargetType = TargetType, TargetId = targetId, ReviewId = targetId,
            // OrderId belongs to order disputes. Review.OrderId supplies display context only.
            TargetUserId = review.ReviewerId
        });
    }

    public async Task<Result<DisputeTargetSummaryDto>> BuildSummaryAsync(
        dispute dispute, CancellationToken cancellationToken = default)
    {
        if (!dispute.ReviewId.HasValue)
            return Result<DisputeTargetSummaryDto>.Fail(DisputeErrors.MissingTarget);
        var review = await reviews.GetByIdAsync(dispute.ReviewId.Value, cancellationToken);
        if (review == null)
            return Result<DisputeTargetSummaryDto>.Fail(ContentDisputeErrors.ReviewNotFound);
        var images = await media.GetByTargetsAsync([review.ReviewId], "Review", cancellationToken);
        if (!images.IsSuccess)
            return Result<DisputeTargetSummaryDto>.Fail(images.Error!);
        var summary = new ReviewDisputeSummaryDto
        {
            ReviewId = review.ReviewId, OrderId = review.OrderId,
            ReviewerId = review.ReviewerId, RevieweeId = review.RevieweeId,
            Rating = review.Rating, Comment = review.Comment,
            Status = (ReviewStatus?)review.ReviewStatus,
            CreatedAt = review.CreatedAt, UpdatedAt = review.UpdatedAt
        };
        if (images.Data != null && images.Data.TryGetValue(review.ReviewId, out var found))
            summary.Images = found;
        return Result<DisputeTargetSummaryDto>.Success(new()
        {
            TargetType = TargetType, TargetId = review.ReviewId, Review = summary
        });
    }
}
