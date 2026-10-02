using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Carts;
using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Services.Carts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Carts
{
    public class CartService : ICartService
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartRealtimeService _cartRealtimeService;
        private readonly CartReadModelBuilder _cartReadModelBuilder;

        public CartService(
            ICartItemRepository cartItemRepository,
            IPostRepository postRepository,
            IUnitOfWork unitOfWork,
            ICartRealtimeService cartRealtimeService,
            CartReadModelBuilder cartReadModelBuilder)
        {
            _cartItemRepository = cartItemRepository;
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _cartRealtimeService = cartRealtimeService;
            _cartReadModelBuilder = cartReadModelBuilder;
        }

        // ================== GET ==================

        public Task<Result<CartResponse>> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return _cartReadModelBuilder.BuildAsync(userId, cancellationToken);
        }

        // ================== ADD ==================

        public async Task<Result<CartItemResponse>> AddAsync(
            Guid userId,
            Guid postId,
            AddToCartRequest request,
            CancellationToken cancellationToken = default)
        {
            var quantity = request?.Quantity ?? 1;
            if (quantity <= 0)
                return Result<CartItemResponse>.Fail(CartErrors.InvalidQuantity);

            var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (post is null)
                return Result<CartItemResponse>.Fail(CartErrors.PostNotFound);

            if (post.Status != PostStatus.Active || post.PostType != PostType.Sell)
                return Result<CartItemResponse>.Fail(CartErrors.PostNotActive);

            if (post.OwnerId == userId)
                return Result<CartItemResponse>.Fail(CartErrors.CannotAddOwnPost);

            if (quantity > post.RemainingQuantity)
                return Result<CartItemResponse>.Fail(
                    CartErrors.QuantityExceedsRemaining(quantity, post.RemainingQuantity));

            var exists = await _cartItemRepository.ExistsAsync(userId, postId, cancellationToken);
            if (exists)
                return Result<CartItemResponse>.Fail(CartErrors.ItemExists);

            var cartItem = new cart_item
            {
                CartItemId = Guid.NewGuid(),
                UserId = userId,
                PostId = postId,
                Quantity = quantity,
                CreatedAt = DateTime.UtcNow
            };

            await _cartItemRepository.AddAsync(cartItem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cartRealtimeService.PublishUpdatedSafelyAsync(userId, cartItem.CreatedAt);
            var created = await _cartItemRepository.GetByIdAsync(cartItem.CartItemId, cancellationToken);
            if (created is null)
                return Result<CartItemResponse>.Fail(CartErrors.ItemNotFound);
            if (created.Post != null)
                await _postRepository.ApplyPriorityAsync(new[] { created.Post }, cancellationToken);

            var mediaResult = await _cartReadModelBuilder.GetPostMediasAsync(
                new[] { postId }, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
                return Result<CartItemResponse>.Fail(mediaResult.Error!);

            return Result<CartItemResponse>.Success(_cartReadModelBuilder.MapItem(created, mediaResult.Data));
        }

        // ================== REMOVE ==================

        public async Task<Result<bool>> RemoveAsync(
            Guid userId,
            Guid cartItemId,
            CancellationToken cancellationToken = default)
        {
            var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId, cancellationToken);
            if (cartItem is null)
                return Result<bool>.Fail(CartErrors.ItemNotFound);

            if (cartItem.UserId != userId)
                return Result<bool>.Fail(CartErrors.Forbidden);

            await _cartItemRepository.DeleteAsync(cartItemId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cartRealtimeService.PublishUpdatedSafelyAsync(userId, DateTime.UtcNow);
            return Result<bool>.Success(true);
        }
    }
}
