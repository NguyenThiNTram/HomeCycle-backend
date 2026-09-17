using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.SupplierMatching;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Application.SupplierMatching.Services;

public sealed class SupplierMatchMonitorService(
    ISupplierMatchMonitorRepository monitorRepository,
    ISupplierMatchEntitlementService entitlements,
    ISupplierMatchService matching,
    IPostRepository posts,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<SupplierMatchMonitorService> logger) : ISupplierMatchMonitorService
{
    public async Task RunBatchAsync(
        int batchSize,
        decimal minimumScore,
        decimal improvementDelta,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        await monitorRepository.DisableClosedOrExpiredAsync(now, cancellationToken);
        var targets = await monitorRepository.GetTargetsAsync(batchSize, cancellationToken);
        foreach (var target in targets)
        {
            try
            {
                await ProcessAsync(target, Math.Clamp(minimumScore, 0m, 10m),
                    Math.Max(0.1m, improvementDelta), now, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Supplier match monitor failed for BuyPostId {BuyPostId}", target.BuyPostId);
            }
        }
    }

    private async Task ProcessAsync(
        SupplierMatchMonitorTarget target,
        decimal minimumScore,
        decimal improvementDelta,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entitlement = await entitlements.GetAsync(target.UserId, cancellationToken);
        if (entitlement.Tier != SupplierMatchTier.Vip)
        {
            if (await monitorRepository.GetStateAsync(target.BuyPostId, cancellationToken) is null)
                await monitorRepository.UpdateStateAsync(
                    target.BuyPostId, target.UserId, 0m, now, cancellationToken);
            await monitorRepository.DisableAsync(target.BuyPostId, now, cancellationToken);
            return;
        }

        var buyPost = await posts.GetDetailByIdAsync(target.BuyPostId, cancellationToken);
        if (buyPost?.Product is null || buyPost.OwnerId != target.UserId ||
            buyPost.PostType != PostType.Buy || buyPost.Status != PostStatus.Active ||
            buyPost.RemainingQuantity <= 0 || buyPost.ExpiryDate <= now.UtcDateTime)
        {
            await monitorRepository.DisableAsync(target.BuyPostId, now, cancellationToken);
            return;
        }

        var response = await matching.MatchBackendOnlyAsync(
            SupplierDemandContextBuilder.FromBuyPost(buyPost), entitlement.ResultLimit, cancellationToken);
        var snapshots = response.Matches
            .Select(match => new SupplierMatchMonitorCandidate(match.SellPost.PostId, match.MatchingScore))
            .ToArray();
        var currentBest = snapshots.Length == 0 ? 0m : snapshots.Max(match => match.Score);
        var state = await monitorRepository.GetStateAsync(target.BuyPostId, cancellationToken);
        if (state is null || !state.IsActive)
        {
            await monitorRepository.InitializeAsync(
                target.BuyPostId, target.UserId, currentBest, snapshots, now, cancellationToken);
            return;
        }

        var improved = response.Matches
            .Where(match => match.MatchingScore >= minimumScore &&
                            match.MatchingScore >= state.LastBestScore + improvementDelta)
            .OrderByDescending(match => match.MatchingScore)
            .ThenBy(match => match.SellPost.PostId)
            .FirstOrDefault();
        if (improved is not null && await monitorRepository.TryClaimImprovementAsync(
                target.BuyPostId, improved.SellPost.PostId, improved.MatchingScore,
                improvementDelta, now, cancellationToken))
        {
            var notification = await notifications.AddPendingAsync(
                new CreateNotificationCommand(
                    target.UserId,
                    "Có nguồn cung phù hợp hơn",
                    $"Một bài bán mới đạt {improved.MatchingScore:0.##}/10 cho nhu cầu thu mua của bạn.",
                    NotificationTargetType.Post,
                    target.BuyPostId),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await notifications.PublishCreatedSafelyAsync(notification);
        }

        await monitorRepository.UpdateStateAsync(
            target.BuyPostId, target.UserId, currentBest, now, cancellationToken);
    }
}
