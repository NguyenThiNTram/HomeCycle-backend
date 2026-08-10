using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Carts;
using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Services.Carts;
using HomeCycle.Application.Interfaces.Services.Posts;
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
        private const string PostMediaTargetType = "Post";

        private readonly ICartItemRepository _cartItemRepository;
        private readonly IPostRepository _postRepository;
        private readonly IMediaService _mediaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CartService(
            ICartItemRepository cartItemRepository,
            IPostRepository postRepository,
            IMediaService mediaService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _cartItemRepository = cartItemRepository;
            _postRepository = postRepository;
            _mediaService = mediaService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // ================== GET ==================

        public async Task<Result<CartResponse>> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var items = await _cartItemRepository.GetByUserAsync(userId, cancellationToken);

            var postIds = items.Select(x => x.PostId).Distinct().ToArray();

            var mediaResult = await _mediaService.GetByTargetsAsync(
                postIds, PostMediaTargetType, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
                return Result<CartResponse>.Fail(mediaResult.Error!);

            var responseItems = items
                .Select(x => MapToResponse(x, mediaResult.Data))
                .ToList();

            var response = new CartResponse
            {
                Items = responseItems,
                TotalQuantity = responseItems.Sum(x => x.Quantity),
                TotalPrice = responseItems.Sum(x => (x.Post.BasePrice ?? 0) * x.Quantity)
            };

            return Result<CartResponse>.Success(response);
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

            var created = await _cartItemRepository.GetByIdAsync(cartItem.CartItemId, cancellationToken);
            if (created is null)
                return Result<CartItemResponse>.Fail(CartErrors.ItemNotFound);

            var mediaResult = await _mediaService.GetByTargetsAsync(
                new[] { postId }, PostMediaTargetType, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
                return Result<CartItemResponse>.Fail(mediaResult.Error!);

            return Result<CartItemResponse>.Success(MapToResponse(created, mediaResult.Data));
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

            return Result<bool>.Success(true);
        }

        // ================== PRIVATE HELPERS ==================

        private CartItemResponse MapToResponse(
            cart_item item,
            IReadOnlyDictionary<Guid, IReadOnlyList<MediaResponse>> mediaByPost)
        {
            var postResponse = _mapper.Map<PostResponse>(item.Post);
            postResponse.Medias = mediaByPost.TryGetValue(item.PostId, out var medias)
                ? medias
                : Array.Empty<MediaResponse>();

            return new CartItemResponse
            {
                CartItemId = item.CartItemId,
                PostId = item.PostId,
                Quantity = item.Quantity,
                AddedAt = item.CreatedAt,
                Post = postResponse
            };
        }
    }
}
