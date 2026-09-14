using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Services.Disputes;

public sealed class PostDisputeTargetHandler(
    IPostRepository posts, IDisputeRepository disputes, IMediaService media) : IDisputeTargetHandler
{
    public DisputeTargetType TargetType => DisputeTargetType.Post;

    public async Task<Result<DisputeTargetCreateContext>> PrepareCreateAsync(
        Guid senderId, Guid targetId, DisputeCategory category, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        // CreateAsync owns the transaction. Serialize duplicate checks on the content row.
        var post = await posts.GetByIdForUpdateAsync(targetId, cancellationToken);
        if (post == null)
            return Result<DisputeTargetCreateContext>.Fail(PostErrors.NotFound);
        if (post.Status is PostStatus.Deleted or PostStatus.Suspended)
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.TargetUnavailable);
        if (post.OwnerId == senderId)
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.SelfReportNotAllowed);
        if (!PostDisputeCategoryPolicy.IsAllowed(category))
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.InvalidCategory);
        if (await disputes.HasOpenDuplicateAsync(senderId, TargetType, targetId, cancellationToken))
            return Result<DisputeTargetCreateContext>.Fail(ContentDisputeErrors.DuplicateOpenReport);

        return Result<DisputeTargetCreateContext>.Success(new()
        {
            TargetType = TargetType, TargetId = targetId, PostId = targetId,
            TargetUserId = post.OwnerId
        });
    }

    public async Task<Result<DisputeTargetSummaryDto>> BuildSummaryAsync(
        dispute dispute, CancellationToken cancellationToken = default)
    {
        if (!dispute.PostId.HasValue)
            return Result<DisputeTargetSummaryDto>.Fail(DisputeErrors.MissingTarget);
        var post = await posts.GetByIdAsync(dispute.PostId.Value, cancellationToken);
        if (post == null)
            return Result<DisputeTargetSummaryDto>.Fail(PostErrors.NotFound);
        var images = await media.GetByTargetsAsync([post.PostId], "Post", cancellationToken);
        if (!images.IsSuccess)
            return Result<DisputeTargetSummaryDto>.Fail(images.Error!);

        var summary = new PostDisputeSummaryDto
        {
            PostId = post.PostId, OwnerId = post.OwnerId,
            ProductName = post.Product?.ProductName, Description = post.Description,
            BasePrice = post.BasePrice, PostType = post.PostType, Status = post.Status,
            CreatedAt = post.CreatedAt, UpdatedAt = post.UpdatedAt
        };
        if (images.Data != null && images.Data.TryGetValue(post.PostId, out var found))
            summary.Images = found;
        return Result<DisputeTargetSummaryDto>.Success(new()
        {
            TargetType = TargetType, TargetId = post.PostId, Post = summary
        });
    }
}
