using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Repositories.Notifications;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRealtimePublisher _realtimePublisher;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
        INotificationRepository notificationRepository,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _realtimePublisher = realtimePublisher;
            _logger = logger;
        }

        public async Task<notification> AddPendingAsync(CreateNotificationCommand command, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command.Title);
            ArgumentException.ThrowIfNullOrWhiteSpace(command.Message);

            if (command.Title.Length > 255)
                throw new ArgumentException(
                    "Tiêu đề thông báo không được vượt quá 255 ký tự.");

            var notification = new notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = command.UserId,
                Title = command.Title.Trim(),
                Message = command.Message.Trim(),
                TargetType = command.TargetType,
                TargetId = command.TargetId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(
                notification,
                cancellationToken);

            return notification;
        }

        public async Task PublishCreatedSafelyAsync(notification notification)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishCreatedAsync(
                    notification.UserId,
                    ToResponse(notification),
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát NotificationCreated cho NotificationId {NotificationId}.",
                    notification.NotificationId);
            }
        }

        public async Task<Result<PagedResult<NotificationResponse>>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            if (request.PageNumber < 1 || request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<NotificationResponse>>.Fail(
                    ValidationErrors.InvalidRequest(
                        "PageNumber phải từ 1 và PageSize phải từ 1 đến 100."));
            }

            var paged = await _notificationRepository.GetByUserAsync(userId, request, cancellationToken);

            return Result<PagedResult<NotificationResponse>>.Success(
                new PagedResult<NotificationResponse>
                {
                    Items = paged.Items
                        .Select(ToResponse)
                        .ToList(),

                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                });
        }

        public async Task<Result<UnreadNotificationCountResponse>>GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var count = await _notificationRepository.CountUnreadAsync(userId, cancellationToken);

            return Result<UnreadNotificationCountResponse>.Success(
                new UnreadNotificationCountResponse
                {
                    UnreadCount = count
                });
        }

        public async Task<Result<NotificationReadResponse>> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _notificationRepository.GetByIdAndUserAsync(notificationId, userId, cancellationToken);

            if (notification is null)
                return Result<NotificationReadResponse>.Fail(NotificationErrors.NotFound);

            if (!notification.IsRead)
                await _notificationRepository.MarkAsReadAsync(notificationId, userId, cancellationToken);

            var unreadCount = await _notificationRepository.CountUnreadAsync(userId, cancellationToken);

            var response = new NotificationReadResponse
            {
                NotificationId = notificationId,
                IsRead = true,
                UnreadCount = unreadCount
            };

            if (!notification.IsRead)
                await PublishReadSafelyAsync(userId, response);

            return Result<NotificationReadResponse>.Success(response);
        }

        public async Task<Result<NotificationsReadAllResponse>>MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var updatedCount = await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);

            var response = new NotificationsReadAllResponse
            {
                UpdatedCount = updatedCount,
                UnreadCount = 0
            };

            if (updatedCount > 0)
                await PublishAllReadSafelyAsync(userId, response);

            return Result<NotificationsReadAllResponse>.Success(response);
        }

        private async Task PublishReadSafelyAsync(Guid userId, NotificationReadResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishReadAsync(userId, response, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát NotificationRead cho NotificationId {NotificationId}.",
                    response.NotificationId);
            }
        }

        private async Task PublishAllReadSafelyAsync(Guid userId, NotificationsReadAllResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishAllReadAsync(userId, response, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát NotificationsReadAll cho UserId {UserId}.",
                    userId);
            }
        }

        private static NotificationResponse ToResponse(notification notification)
        {
            return new NotificationResponse
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Message = notification.Message,
                TargetType = notification.TargetType,
                TargetId = notification.TargetId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

    }
}
