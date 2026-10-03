using AutoMapper;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Carts
{
    // Dựng giỏ hàng từ database. Dùng chung cho GET /cart và event CartUpdated
    // để hai nơi luôn trả cùng dữ liệu.
    public sealed class CartReadModelBuilder
    {
        private const string PostMediaTargetType = "Post";

        private readonly ICartItemRepository _cartItemRepository;
        private readonly IPostRepository _postRepository;
        private readonly IMediaService _mediaService;
        private readonly IMapper _mapper;

        public CartReadModelBuilder(
            ICartItemRepository cartItemRepository,
            IPostRepository postRepository,
            IMediaService mediaService,
            IMapper mapper)
        {
            _cartItemRepository = cartItemRepository;
            _postRepository = postRepository;
            _mediaService = mediaService;
            _mapper = mapper;
        }

        public async Task<Result<CartResponse>> BuildAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var items = await _cartItemRepository.GetByUserAsync(userId, cancellationToken);
            await _postRepository.ApplyPriorityAsync(
                items.Where(x => x.Post != null).Select(x => x.Post!).ToList(), cancellationToken);

            var postIds = items.Select(x => x.PostId).Distinct().ToArray();

            var mediaResult = await GetPostMediasAsync(postIds, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
                return Result<CartResponse>.Fail(mediaResult.Error!);

            var responseItems = items
                .Select(x => MapItem(x, mediaResult.Data))
                .ToList();

            return Result<CartResponse>.Success(new CartResponse
            {
                Items = responseItems,
                TotalQuantity = responseItems.Sum(x => x.Quantity),
                TotalPrice = responseItems.Sum(x => (x.Post.BasePrice ?? 0) * x.Quantity)
            });
        }

        public Task<Result<IReadOnlyDictionary<Guid, IReadOnlyList<MediaResponse>>>> GetPostMediasAsync(
            IReadOnlyCollection<Guid> postIds,
            CancellationToken cancellationToken = default)
        {
            return _mediaService.GetByTargetsAsync(postIds, PostMediaTargetType, cancellationToken);
        }

        public CartItemResponse MapItem(
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
