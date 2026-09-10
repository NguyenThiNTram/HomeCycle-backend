using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Application.Services.Offers;

public partial class OfferService
{
    private async Task<Result<OfferResponse>> CreateCoreAsync(Guid userId, CreateOfferRequest request, bool sellerRequest, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<OfferResponse>.Fail(ToValidationError(validation));
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _postRepository.LockAsync(request.PostId, request.BuyPostId, ct);
            var sell = await _postRepository.GetByIdAsync(request.PostId, ct);
            if (sell == null) return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);
            // New transactions always reference the real listed product. Old Buy-target offers remain readable.
            if (sell.PostType != PostType.Sell) return Result<OfferResponse>.Fail(PostErrors.InvalidPostType);
            var sender = await _userRepository.GetByIdAsync(userId, ct);
            if (sender?.Status != UserStatus.Active) return Result<OfferResponse>.Fail(OfferErrors.UserNotActive);
            if (sender.Role is not (UserRole.Personal or UserRole.Business)) return Result<OfferResponse>.Fail(OfferErrors.RoleNotAllowed);
            post? buy = null;
            if (request.BuyPostId.HasValue)
            {
                buy = await _postRepository.GetByIdAsync(request.BuyPostId.Value, ct);
                if (buy?.PostType != PostType.Buy) return Result<OfferResponse>.Fail(PostErrors.InvalidPostType);
                var business = await _userRepository.GetByIdAsync(buy.OwnerId, ct);
                if (business?.Role != UserRole.Business || business.Status != UserStatus.Active) return Result<OfferResponse>.Fail(OfferErrors.RoleNotAllowed);
                if (!sellerRequest && (sender.Role != UserRole.Business || buy.OwnerId != userId)) return Result<OfferResponse>.Fail(OfferErrors.Forbidden);
            }
            if (sellerRequest && (buy == null || sender.Role != UserRole.Personal || sell.OwnerId != userId))
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);
            if (!sellerRequest && sell.OwnerId == userId) return Result<OfferResponse>.Fail(OfferErrors.CannotOfferOwnPost);
            var receiverId = sellerRequest ? buy!.OwnerId : sell.OwnerId;
            var receiver = await _userRepository.GetByIdAsync(receiverId, ct);
            if (receiver?.Status != UserStatus.Active) return Result<OfferResponse>.Fail(OfferErrors.UserNotActive);
            if (buy != null)
            {
                var seller = sellerRequest ? sender : receiver;
                if (seller.Role != UserRole.Personal || seller.UserId != sell.OwnerId) return Result<OfferResponse>.Fail(OfferErrors.RoleNotAllowed);
            }
            if (receiverId == userId) return Result<OfferResponse>.Fail(OfferErrors.CannotOfferOwnPost);
            var entity = new offer {
                OfferId = Guid.NewGuid(), PostId = sell.PostId, BuyPostId = buy?.PostId,
                SenderId = userId, ReceiverId = receiverId, OfferPrice = request.OfferPrice,
                OfferQuantity = request.OfferQuantity, OfferStatus = OfferStatus.Pending, Version = 1, CreatedAt = DateTime.UtcNow
            };
            var error = await ValidateNewOfferAsync(entity, sell, request.OfferPrice, request.OfferQuantity, ct);
            if (error != null) return Result<OfferResponse>.Fail(error);
            if (await _offerRepository.ExistsPendingByPostAndSenderAsync(sell.PostId, userId, receiverId, entity.BuyPostId, ct))
                return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending);
            await _offerRepository.AddAsync(entity, ct);
            var notification = await AddOfferNotificationPendingAsync(entity, userId, "Bạn có đề nghị mới",
                sellerRequest ? "Bạn vừa nhận được một chào hàng từ người bán." : "Bạn vừa nhận được đề nghị mua sản phẩm.", ct);
            await _unitOfWork.SaveChangesAsync(ct);
            var created = await _offerRepository.GetByIdAsync(entity.OfferId, ct);
            _unitOfWork.RegisterAfterCommit(async () => {
                await PublishOfferCreatedSafelyAsync(created!);
                await PublishOfferUpdatedSafelyAsync(created!);
                await _notificationService.PublishCreatedSafelyAsync(notification);
            });
            await _unitOfWork.CommitTransactionAsync(ct);
            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(created));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending); }
        finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }

    private async Task<offer?> LockOfferAsync(Guid offerId, CancellationToken ct)
    {
        var snapshot = await _offerRepository.GetByIdAsync(offerId, ct);
        if (snapshot == null) return null;
        await _postRepository.LockAsync(snapshot.PostId, snapshot.BuyPostId, ct);
        return await _offerRepository.GetByIdForUpdateAsync(offerId, ct);
    }

    private async Task<Error?> ValidateNewOfferAsync(offer offer, post sell, decimal price, int quantity, CancellationToken ct)
    {
        if (sell.PostType != PostType.Sell) return PostErrors.InvalidPostType;
        var capacity = await _postRepository.ValidateCapacityAsync(offer, quantity, null, true, ct);
        if (capacity != null) return capacity;
        return _offerTermsPolicy.Validate(sell, price, quantity, offer.BuyPostId.HasValue);
    }
}
