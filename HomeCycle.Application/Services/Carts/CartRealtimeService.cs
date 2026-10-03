using HomeCycle.Application.DTOs.Responses.Carts;
using HomeCycle.Application.Interfaces.Repositories.Carts;
using HomeCycle.Application.Interfaces.Services.Carts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Carts
{
    public sealed class CartRealtimeService : ICartRealtimeService
    {
        private readonly ICartRealtimePublisher _realtimePublisher;
        private readonly CartReadModelBuilder _cartReadModelBuilder;
        private readonly ILogger<CartRealtimeService> _logger;

        public CartRealtimeService(
            ICartRealtimePublisher realtimePublisher,
            CartReadModelBuilder cartReadModelBuilder,
            ILogger<CartRealtimeService> logger)
        {
            _realtimePublisher = realtimePublisher;
            _cartReadModelBuilder = cartReadModelBuilder;
            _logger = logger;
        }

        // Gọi sau khi thay đổi giỏ hàng đã được lưu. Tham số updatedAt của nơi gọi được giữ để không đổi
        // interface; UpdatedAt của event là thời điểm đọc snapshot nên luôn tăng theo thứ tự dữ liệu.
        public async Task PublishUpdatedSafelyAsync(Guid userId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var snapshotAt = DateTime.UtcNow;
                var response = await BuildResponseAsync(userId, snapshotAt, timeout.Token);

                await _realtimePublisher.PublishUpdatedAsync(
                    userId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể phát CartUpdated cho User {UserId}.", userId);
            }
        }

        private async Task<CartUpdatedResponse> BuildResponseAsync(
            Guid userId,
            DateTime snapshotAt,
            CancellationToken cancellationToken)
        {
            try
            {
                var cartResult = await _cartReadModelBuilder.BuildAsync(userId, cancellationToken);

                if (cartResult.IsSuccess && cartResult.Data != null)
                {
                    var cart = cartResult.Data;

                    return new CartUpdatedResponse
                    {
                        UpdatedAt = snapshotAt,
                        TotalQuantity = cart.TotalQuantity,
                        TotalPrice = cart.TotalPrice,
                        Items = cart.Items.Select(ToRealtimeItem).ToList()
                    };
                }

                _logger.LogWarning(
                    "Không dựng được snapshot giỏ hàng cho User {UserId}; chỉ phát tín hiệu tải lại.",
                    userId);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Không dựng được snapshot giỏ hàng cho User {UserId}; chỉ phát tín hiệu tải lại.",
                    userId);
            }

            return new CartUpdatedResponse { UpdatedAt = snapshotAt };
        }

        private static CartRealtimeItemResponse ToRealtimeItem(CartItemResponse item)
        {
            var post = item.Post;

            return new CartRealtimeItemResponse
            {
                CartItemId = item.CartItemId,
                PostId = item.PostId,
                Quantity = item.Quantity,
                AddedAt = item.AddedAt,
                OwnerId = post.OwnerId,
                ProductName = post.ProductName,
                ProductTypeName = post.ProductTypeName,
                CategoryName = post.CategoryName,
                BrandName = post.BrandName,
                BasePrice = post.BasePrice,
                RemainingQuantity = post.RemainingQuantity,
                PostType = post.PostType?.ToString(),
                Status = post.Status?.ToString(),
                ThumbnailUrl = post.Medias?
                    .OrderBy(media => media.DisplayOrder)
                    .Select(media => media.Url)
                    .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url))
            };
        }
    }
}
