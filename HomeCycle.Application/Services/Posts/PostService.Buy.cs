using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.DTOs.Responses.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Services;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Application.Services.Posts;

public partial class PostService
{
    public async Task<Result<PostResponse>> CreateBuyPostAsync(Guid ownerId, CreateBuyPostRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createBuyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(validation.ToString()));
        var roleError = await ValidateCreateRoleAsync(ownerId, UserRole.Business, cancellationToken);
        if (roleError != null) return Result<PostResponse>.Fail(roleError);
        var now = DateTime.UtcNow;
        var entity = _mapper.Map<post>(request);
        entity.PostId = Guid.NewGuid(); entity.OwnerId = ownerId;
        entity.Description = request.Description.Trim(); entity.PostType = PostType.Buy;
        entity.IsBusinessPosting = true; entity.Quantity = request.Quantity ?? 1;
        entity.RemainingQuantity = entity.Quantity; entity.CreatedAt = now; entity.UpdatedAt = now;
        entity.Status = PostStatus.Active; entity.ExpiryDate = request.ExpiryDate?.ToUniversalTime() ?? now.AddDays(30);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _postRepository.AddAsync(entity, cancellationToken);
            var requirement = _mapper.Map<ProductRequirementRequest>(request);
            requirement.ProductName = request.Title.Trim(); requirement.AttributeValues ??= [];
            var product = await _productService.PrepareForRequirementAsync(entity.PostId, requirement, cancellationToken);
            if (!product.IsSuccess) return Result<PostResponse>.Fail(product.Error!);

            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.BusinessOperation,
                Action = AuditActions.PostCreate,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                UserId = ownerId,
                TargetType = AuditTargetTypes.Post,
                TargetId = entity.PostId,
                NewValues = new Dictionary<string, object?>
                {
                    ["postType"] = entity.PostType.ToString(),
                    ["status"] = entity.Status.ToString(),
                    ["minExpectedPrice"] = entity.MinExpectedPrice,
                    ["basePrice"] = entity.BasePrice,
                    ["quantity"] = entity.Quantity
                }
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        var saved = await _postRepository.GetByIdAsync(entity.PostId, cancellationToken);
        await SendPostCreatedNotificationAsync(ownerId, entity.PostId, "Đã tạo tin thu mua", "Tin thu mua của bạn đã được đăng tải.", cancellationToken);
        await TryWarmSupplierMatchesAsync(ownerId, entity.PostId, cancellationToken);
        return Result<PostResponse>.Success(_mapper.Map<PostResponse>(saved));
    }

    public async Task<Result<PostResponse>> UpdateBuyPostAsync(Guid ownerId, Guid postId, UpdateBuyPostRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateBuyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(validation.ToString()));
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);
            var current = await _postRepository.GetDetailByIdAsync(postId, cancellationToken);
            if (current == null) return Result<PostResponse>.Fail(PostErrors.NotFound);
            if (current.OwnerId != ownerId) return Result<PostResponse>.Fail(PostErrors.Forbidden);
            if (current.PostType != PostType.Buy) return Result<PostResponse>.Fail(PostErrors.InvalidPostType);
            if (current.Status is PostStatus.Deleted or PostStatus.Suspended ||
                (current.Status == PostStatus.Closed && (request.Quantity ?? current.Quantity) <= current.Quantity))
                return Result<PostResponse>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
            if (current.ExpiryDate <= DateTime.UtcNow) return Result<PostResponse>.Fail(PostErrors.PostExpired);
            if (current.Product == null) return Result<PostResponse>.Fail(ProductErrors.ProductNotFound);
            var p = current.Product!;
            var merged = new CreateBuyPostRequest {
                Title = p.ProductName ?? "", Description = current.Description ?? "", CategoryId = p.CategoryId,
                ProductTypeId = p.ProductTypeId, BrandId = p.BrandId, ModelNumber = p.ModelNumber,
                FunctionalityStatus = p.FunctionalityStatus,
                UsageDuration = p.UsageDuration, DamageLevel = p.DamageLevel, StreetAddress = current.StreetAddress,
                Ward = current.Ward, City = current.City, PriorityLevel = current.PriorityLevel,
                PriceFrom = current.MinExpectedPrice, PriceTo = current.BasePrice, Quantity = current.Quantity, ExpiryDate = current.ExpiryDate,
                AttributeValues = p.Product_Attribute_Values.Select(v => new ProductAttributeValueRequest {
                    AttributeId = v.AttributeId, OptionId = v.OptionId, ValueText = v.ValueText, ValueNumber = v.ValueNumber, ValueBoolean = v.ValueBoolean
                }).ToList()
            };
            var before = System.Text.Json.JsonSerializer.SerializeToElement(merged);
            var previousMinExpectedPrice = current.MinExpectedPrice;
            var previousBasePrice = current.BasePrice;
            var previousQuantity = current.Quantity;
            var previousPostStatus = current.Status;
            var previousExpiryDate = current.ExpiryDate;
            foreach (var name in request.ChangedProperties)
                typeof(CreateBuyPostRequest).GetProperty(name)!.SetValue(merged, typeof(UpdateBuyPostRequest).GetProperty(name)!.GetValue(request));
            if (merged.CategoryId == null && !request.ChangedProperties.Contains(nameof(request.ProductTypeId))) merged.ProductTypeId = null;
            if (merged.ProductTypeId != p.ProductTypeId && !request.ChangedProperties.Contains(nameof(request.AttributeValues))) merged.AttributeValues = [];
            merged.AttributeValues ??= [];
            var effectiveValidation = await _createBuyValidator.ValidateAsync(merged, cancellationToken);
            if (!effectiveValidation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(effectiveValidation.ToString()));
            if (merged.ExpiryDate?.ToUniversalTime() > current.CreatedAt.AddMonths(6))
                return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest("Thời hạn tối đa là 6 tháng từ ngày tạo tin."));
            var allocated = current.Quantity - current.RemainingQuantity;
            var reserved = await _postRepository.GetReservedQuantityAsync(postId, null, cancellationToken);
            if (merged.Quantity < allocated + reserved)
                return Result<PostResponse>.Fail(PostErrors.InvalidUpdateQuantity(allocated + reserved, merged.Quantity!.Value));
            var materialFields = new[] { "Title", "BrandId", "CategoryId", "ProductTypeId", "ModelNumber", "FunctionalityStatus", "UsageDuration", "DamageLevel", "AttributeValues", "PriceFrom", "PriceTo" };
            var after = System.Text.Json.JsonSerializer.SerializeToElement(merged);
            var material = materialFields.Any(name => before.GetProperty(name).GetRawText() != after.GetProperty(name).GetRawText());
            if (material && await _postRepository.HasUnfinishedTransactionsAsync(postId, cancellationToken))
                return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest("Không thể đổi tiêu chí khi tin đang có giao dịch chưa hoàn tất."));
            current.Description = merged.Description.Trim(); current.Quantity = merged.Quantity!.Value;
            current.RemainingQuantity = current.Quantity - allocated;
            current.MinExpectedPrice = merged.PriceFrom; current.BasePrice = merged.PriceTo;
            current.StreetAddress = merged.StreetAddress; current.Ward = merged.Ward; current.City = merged.City;
            current.PriorityLevel = merged.PriorityLevel; current.ExpiryDate = merged.ExpiryDate?.ToUniversalTime(); current.UpdatedAt = DateTime.UtcNow;
            if (current.RemainingQuantity == 0) current.Status = PostStatus.Closed;
            else if (current.Status == PostStatus.Closed && current.Quantity > previousQuantity)
                current.Status = PostStatus.Active;
            await _postRepository.UpdateAsync(current, cancellationToken);
            var requirement = _mapper.Map<ProductRequirementRequest>(merged); requirement.ProductName = merged.Title.Trim();
            // Merge preserved attributes above, so this operation always saves the complete final requirement.
            var product = await _productService.UpdateForRequirementAsync(postId, requirement, cancellationToken);
            if (!product.IsSuccess) return Result<PostResponse>.Fail(product.Error!);
            if (material || previousQuantity != current.Quantity || current.Status == PostStatus.Closed)
                await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);

            var buyPostAuditDiff = new AuditDiffBuilder()
                .Add("minExpectedPrice", previousMinExpectedPrice, current.MinExpectedPrice)
                .Add("basePrice", previousBasePrice, current.BasePrice)
                .Add("quantity", previousQuantity, current.Quantity)
                .Add("status", previousPostStatus.ToString(), current.Status.ToString())
                .Add("expiryDate", previousExpiryDate, current.ExpiryDate);

            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.BusinessOperation,
                Action = AuditActions.PostUpdate,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                UserId = ownerId,
                TargetType = AuditTargetTypes.Post,
                TargetId = current.PostId,
                OldValues = buyPostAuditDiff.OldValues,
                NewValues = buyPostAuditDiff.NewValues
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<PostResponse>.Success(_mapper.Map<PostResponse>(await _postRepository.GetByIdAsync(postId, cancellationToken)));
        }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }

    public Task<Result<bool>> DeleteBuyPostAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(ownerId, postId, PostStatus.Deleted, true, cancellationToken);

    private async Task<Result<bool>> DeleteBuyPostAsAdminAsync(Guid postId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var post = await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);
            if (post is null || post.Status == PostStatus.Deleted)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (await _postRepository.HasUnfinishedTransactionsAsync(postId, cancellationToken))
                return Result<bool>.Fail(ValidationErrors.InvalidRequest("Bài đăng đang có giao dịch chưa hoàn tất."));
            var previousPostStatus = post.Status;
            post.Status = PostStatus.Deleted;
            post.UpdatedAt = DateTime.UtcNow;
            await _postRepository.UpdateAsync(post, cancellationToken);
            await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);

            var deleteBuyPostAuditDiff = new AuditDiffBuilder()
                .Add("status", previousPostStatus.ToString(), post.Status.ToString());

            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.Administration,
                Action = AuditActions.PostDelete,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                TargetType = AuditTargetTypes.Post,
                TargetId = post.PostId,
                OldValues = deleteBuyPostAuditDiff.OldValues,
                NewValues = deleteBuyPostAuditDiff.NewValues
            }, cancellationToken);


            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        finally
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private async Task<Result<bool>> ChangeLifecycleAsync(Guid ownerId, Guid postId, PostStatus status, bool buyOnly, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var p = await _postRepository.GetByIdForUpdateAsync(postId, ct);
            if (p == null) return Result<bool>.Fail(PostErrors.NotFound);
            if (p.OwnerId != ownerId) return Result<bool>.Fail(PostErrors.Forbidden);
            if (buyOnly && p.PostType != PostType.Buy) return Result<bool>.Fail(PostErrors.InvalidPostType);
            if (p.Status is PostStatus.Deleted or PostStatus.Suspended) return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
            if (status is PostStatus.Active or PostStatus.Deleted && p.ExpiryDate <= DateTime.UtcNow) return Result<bool>.Fail(PostErrors.PostExpired);
            if (status == PostStatus.Active && (p.Status != PostStatus.Closed || p.RemainingQuantity <= 0)) return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
            if (status == PostStatus.Deleted && await _postRepository.HasUnfinishedTransactionsAsync(postId, ct))
                return Result<bool>.Fail(ValidationErrors.InvalidRequest("Bài đăng đang có giao dịch chưa hoàn tất. Bạn có thể đóng nhận đề nghị mới."));
            var previousPostStatus = p.Status;
            p.Status = status; p.UpdatedAt = DateTime.UtcNow;
            await _postRepository.UpdateAsync(p, ct);
            if (status != PostStatus.Active) await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, ct);

            var lifecycleAuditAction = status switch
            {
                PostStatus.Closed => AuditActions.PostClose,
                PostStatus.Active => AuditActions.PostReactivate,
                PostStatus.Deleted => AuditActions.PostDelete,
                _ => throw new InvalidOperationException($"Unsupported audited post status: {status}.")
            };

            var postLifecycleAuditDiff = new AuditDiffBuilder()
                .Add("status", previousPostStatus.ToString(), p.Status.ToString());

            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.BusinessOperation,
                Action = lifecycleAuditAction,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                UserId = ownerId,
                TargetType = AuditTargetTypes.Post,
                TargetId = p.PostId,
                OldValues = postLifecycleAuditDiff.OldValues,
                NewValues = postLifecycleAuditDiff.NewValues
            }, ct);

            await _unitOfWork.SaveChangesAsync(ct); await _unitOfWork.CommitTransactionAsync(ct);
            return Result<bool>.Success(true);
        }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }

    public async Task ExpireBuyPostsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _postRepository.GetExpiredBuyPostIdsAsync(DateTime.UtcNow, 100, cancellationToken);
        foreach (var id in ids)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var p = await _postRepository.GetByIdForUpdateAsync(id, cancellationToken);
                if (p?.Status == PostStatus.Active && p.ExpiryDate <= DateTime.UtcNow)
                {
                    p.Status = PostStatus.Closed; p.UpdatedAt = DateTime.UtcNow;
                    await _postRepository.UpdateAsync(p, cancellationToken);
                    await _offerRepository.ClosePendingByPostAsync(id, OfferStatus.Expired, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }
    }

    public async Task<Result<PagedResult<BuyPostMatchResponse>>> GetMatchesAsync(Guid ownerId, Guid buyPostId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
        if (buy == null || buy.Product == null || buy.User?.Status != UserStatus.Active || buy.PostType != PostType.Buy || !TradingPostRules.IsAvailable(buy))
            return Result<PagedResult<BuyPostMatchResponse>>.Fail(PostErrors.NotFound);
        if (buy.OwnerId != ownerId)
            return Result<PagedResult<BuyPostMatchResponse>>.Fail(PostErrors.Forbidden);
        var matched = await _supplierMatchService.MatchAsync(
            SupplierDemandContextBuilder.FromBuyPost(buy),
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);
        return Result<PagedResult<BuyPostMatchResponse>>.Success(new PagedResult<BuyPostMatchResponse> {
            Items = matched.Matches,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = matched.CandidateCount
        });
    }

    public async Task<Result<SupplierMatchResponse>> GetSupplierMatchesAsync(
        Guid ownerId,
        Guid buyPostId,
        CancellationToken cancellationToken = default)
    {
        var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
        if (buy == null || buy.Product == null || buy.User?.Status != UserStatus.Active ||
            buy.PostType != PostType.Buy || !TradingPostRules.IsAvailable(buy))
            return Result<SupplierMatchResponse>.Fail(PostErrors.NotFound);
        if (buy.OwnerId != ownerId)
            return Result<SupplierMatchResponse>.Fail(PostErrors.Forbidden);

        var response = await _supplierMatchService.MatchAsync(
            SupplierDemandContextBuilder.FromBuyPost(buy), 0, int.MaxValue, cancellationToken);
        response.BuyPostId = buyPostId;
        return Result<SupplierMatchResponse>.Success(response);
    }

    private async Task TryWarmSupplierMatchesAsync(Guid ownerId, Guid buyPostId, CancellationToken cancellationToken)
    {
        try
        {
            await GetSupplierMatchesAsync(ownerId, buyPostId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Bài mua đã commit; request bị hủy chỉ dừng bước warm cache.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Không thể tạo supplier matches ban đầu cho BuyPostId {BuyPostId}", buyPostId);
        }
    }
}
