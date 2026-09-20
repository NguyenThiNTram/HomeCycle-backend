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
        private readonly ILogger<CartRealtimeService> _logger;

        public CartRealtimeService(ICartRealtimePublisher realtimePublisher, ILogger<CartRealtimeService> logger)
        {
            _realtimePublisher = realtimePublisher;
            _logger = logger;
        }

        public async Task PublishUpdatedSafelyAsync(Guid userId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishUpdatedAsync(
                    userId,
                    new CartUpdatedResponse
                    {
                        UpdatedAt = updatedAt
                    },
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể phát CartUpdated cho User {UserId}.", userId);
            }
        }
    }
}
