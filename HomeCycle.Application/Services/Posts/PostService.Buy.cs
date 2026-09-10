using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;

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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        var saved = await _postRepository.GetByIdAsync(entity.PostId, cancellationToken);
        await SendPostCreatedNotificationAsync(ownerId, entity.PostId, "Đã tạo tin thu mua", "Tin thu mua của bạn đã được đăng tải.", cancellationToken);
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
            if (current.Status is PostStatus.Deleted or PostStatus.Suspended or PostStatus.Closed)
                return Result<PostResponse>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
            if (current.ExpiryDate <= DateTime.UtcNow) return Result<PostResponse>.Fail(PostErrors.PostExpired);
            if (current.Product == null) return Result<PostResponse>.Fail(ProductErrors.ProductNotFound);
            var p = current.Product!;
            var merged = new CreateBuyPostRequest {
                Title = p.ProductName ?? "", Description = current.Description ?? "", CategoryId = p.CategoryId,
                ProductTypeId = p.ProductTypeId, BrandId = p.BrandId, FunctionalityStatus = p.FunctionalityStatus,
                UsageDuration = p.UsageDuration, DamageLevel = p.DamageLevel, StreetAddress = current.StreetAddress,
                Ward = current.Ward, City = current.City, PriorityLevel = current.PriorityLevel,
                PriceFrom = current.MinExpectedPrice, PriceTo = current.BasePrice, Quantity = current.Quantity, ExpiryDate = current.ExpiryDate,
                AttributeValues = p.Product_Attribute_Values.Select(v => new ProductAttributeValueRequest {
                    AttributeId = v.AttributeId, OptionId = v.OptionId, ValueText = v.ValueText, ValueNumber = v.ValueNumber, ValueBoolean = v.ValueBoolean
                }).ToList()
            };
            var before = System.Text.Json.JsonSerializer.SerializeToElement(merged);
            var previousQuantity = current.Quantity;
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
            var materialFields = new[] { "Title", "BrandId", "CategoryId", "ProductTypeId", "FunctionalityStatus", "UsageDuration", "DamageLevel", "AttributeValues", "PriceFrom", "PriceTo" };
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
            await _postRepository.UpdateAsync(current, cancellationToken);
            var requirement = _mapper.Map<ProductRequirementRequest>(merged); requirement.ProductName = merged.Title.Trim();
            // Merge preserved attributes above, so this operation always saves the complete final requirement.
            var product = await _productService.UpdateForRequirementAsync(postId, requirement, cancellationToken);
            if (!product.IsSuccess) return Result<PostResponse>.Fail(product.Error!);
            if (material || previousQuantity != current.Quantity || current.Status == PostStatus.Closed)
                await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result<PostResponse>.Success(_mapper.Map<PostResponse>(await _postRepository.GetByIdAsync(postId, cancellationToken)));
        }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }

    public Task<Result<bool>> DeleteBuyPostAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default) =>
        ChangeLifecycleAsync(ownerId, postId, PostStatus.Deleted, true, cancellationToken);

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
            p.Status = status; p.UpdatedAt = DateTime.UtcNow;
            await _postRepository.UpdateAsync(p, ct);
            if (status != PostStatus.Active) await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, ct);
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

    public async Task<Result<PagedResult<BuyPostMatchResponse>>> GetMatchesAsync(Guid buyPostId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
        if (buy == null || buy.Product == null || buy.User?.Status != UserStatus.Active || buy.PostType != PostType.Buy || !TradingPostRules.IsAvailable(buy))
            return Result<PagedResult<BuyPostMatchResponse>>.Fail(PostErrors.NotFound);
        var page = await _postRepository.GetMatchesAsync(buy, request, cancellationToken);
        var media = await _mediaService.GetByTargetsAsync(page.Items.Select(p => p.PostId).ToArray(), PostMediaTargetType, cancellationToken);
        var items = page.Items.Select(p => {
            var response = _mapper.Map<PostResponse>(p);
            if (media.IsSuccess && media.Data!.TryGetValue(p.PostId, out var files)) response.Medias = files;
            return new BuyPostMatchResponse { SellPost = response, MatchSummary = BuyPostMatching.Evaluate(buy, p) };
        }).ToList();
        return Result<PagedResult<BuyPostMatchResponse>>.Success(new PagedResult<BuyPostMatchResponse> {
            Items = items, PageNumber = page.PageNumber, PageSize = page.PageSize, TotalCount = page.TotalCount
        });
    }
}
