using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Negotiates
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IOfferRepository _offerRepository;
        private readonly IChatRealtimePublisher _realtimePublisher;
        private readonly IMapper _mapper;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            INegotiationRepository negotiationRepository,
            IOfferRepository offerRepository,
            IChatRealtimePublisher realtimePublisher,
            IMapper mapper,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _negotiationRepository = negotiationRepository;
            _offerRepository = offerRepository;
            _realtimePublisher = realtimePublisher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<ConversationListItemResponse>> GetByIdAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation is null)
                return Result<ConversationListItemResponse>.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(conversation, userId))
                return Result<ConversationListItemResponse>.Fail(NegotiationErrors.Forbidden);

            var latestMessages = await _messageRepository.GetLatestByConversationsAsync(new[] { conversationId }, cancellationToken);

            latestMessages.TryGetValue(conversationId, out var latestMessage);

            var unreadCount = await _messageRepository.CountUnreadByConversationForUserAsync(conversationId, userId, cancellationToken);

            var response = ToConversationListItemResponse(conversation, userId, latestMessage, unreadCount);

            return Result<ConversationListItemResponse>.Success(response);
        }

        public async Task<Result<PagedResult<ConversationListItemResponse>>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paginationError = GetPaginationError(request);

            if (paginationError is not null)
                return Result<PagedResult<ConversationListItemResponse>>.Fail(ValidationErrors.InvalidRequest(paginationError));

            var pagedConversations = await _conversationRepository.GetMineAsync(userId, request, cancellationToken);

            if (pagedConversations.Items.Count == 0)
            {
                return Result<PagedResult<ConversationListItemResponse>>.Success(
                    new PagedResult<ConversationListItemResponse>
                    {
                        Items = new List<ConversationListItemResponse>(),
                        PageNumber = pagedConversations.PageNumber,
                        PageSize = pagedConversations.PageSize,
                        TotalCount = pagedConversations.TotalCount
                    });
            }

            var conversationIds = pagedConversations.Items
                .Select(x => x.ConversationId)
                .ToList();

            // Hai query theo batch, không query riêng cho từng Conversation.
            var latestMessages =
                await _messageRepository.GetLatestByConversationsAsync(conversationIds, cancellationToken);

            var unreadCounts = await _messageRepository.GetUnreadCountsByConversationsAsync(userId, conversationIds, cancellationToken);

            var items = pagedConversations.Items
                .Select(conversation =>
                {
                    latestMessages.TryGetValue(conversation.ConversationId, out var latestMessage);
                    unreadCounts.TryGetValue(conversation.ConversationId, out var unreadCount);

                    return ToConversationListItemResponse(conversation, userId, latestMessage, unreadCount);
                })
                .ToList();

            return Result<PagedResult<ConversationListItemResponse>>.Success(
                new PagedResult<ConversationListItemResponse>
                {
                    Items = items,
                    PageNumber = pagedConversations.PageNumber,
                    PageSize = pagedConversations.PageSize,
                    TotalCount = pagedConversations.TotalCount
                }
            );
        }

        public async Task<Result<PagedResult<MessageResponse>>> GetTimelineAsync(Guid userId, Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paginationError = GetPaginationError(request);

            if (paginationError is not null)
                return Result<PagedResult<MessageResponse>>.Fail(ValidationErrors.InvalidRequest(paginationError));

            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);

            if (conversation is null)
                return Result<PagedResult<MessageResponse>>.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(conversation, userId))
                return Result<PagedResult<MessageResponse>>.Fail(NegotiationErrors.Forbidden);

            var pagedMessages = await _messageRepository.GetByConversationIdAsync(conversationId, request, cancellationToken);

            return Result<PagedResult<MessageResponse>>.Success(
                new PagedResult<MessageResponse>
                {
                    Items = pagedMessages.Items
                        .Select(x => _mapper.Map<MessageResponse>(x))
                        .ToList(),

                    PageNumber = pagedMessages.PageNumber,
                    PageSize = pagedMessages.PageSize,
                    TotalCount = pagedMessages.TotalCount
                }
            );
        }

        public async Task<Result<PagedResult<NegotiationListItemResponse>>> GetNegotiationsAsync(Guid userId, Guid conversationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paginationError = GetPaginationError(request);

            if (paginationError is not null)
                return Result<PagedResult<NegotiationListItemResponse>>.Fail(ValidationErrors.InvalidRequest(paginationError));

            var conversation =
                await _conversationRepository.GetByIdAsync(
                    conversationId,
                    cancellationToken);

            if (conversation is null)
                return Result<PagedResult<NegotiationListItemResponse>>.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(conversation, userId))
                return Result<PagedResult<NegotiationListItemResponse>>.Fail(NegotiationErrors.Forbidden);

            var pagedNegotiations = await _negotiationRepository.GetByConversationIdAsync(conversationId, request, cancellationToken);

            var unreadByNegotiation = new Dictionary<Guid, int>();

            if (pagedNegotiations.Items.Count > 0)
            {
                var unreadDetails = await _messageRepository.GetUnreadCountsDetailAsync(conversationId, conversation.UserOneId, conversation.UserTwoId, cancellationToken);

                if (unreadDetails.TryGetValue(userId, out var userUnread))
                {
                    foreach (var unread in userUnread.UnreadByNegotiation)
                    {
                        unreadByNegotiation[unread.Key] = unread.Value;
                    }
                }
            }

            var items = pagedNegotiations.Items
                .Select(negotiation =>
                {
                    unreadByNegotiation.TryGetValue(negotiation.NegotiationId, out var unreadCount);

                    return ToNegotiationListItemResponse(negotiation, userId, unreadCount);
                })
                .ToList();

            return Result<PagedResult<NegotiationListItemResponse>>.Success(
                new PagedResult<NegotiationListItemResponse>
                {
                    Items = items,
                    PageNumber = pagedNegotiations.PageNumber,
                    PageSize = pagedNegotiations.PageSize,
                    TotalCount = pagedNegotiations.TotalCount
                }
            );
        }

        public async Task<Result> MarkAsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);

            if (conversation is null)
                return Result.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(conversation, userId))
                return Result.Fail(NegotiationErrors.Forbidden);

            var readAt = DateTime.UtcNow;

            var updatedCount =
                await _messageRepository.MarkConversationAsReadAsync(conversationId, userId, readAt, cancellationToken);

            // Endpoint idempotent.
            if (updatedCount == 0)
                return Result.Success();

            var readResponse = new ConversationMessagesReadResponse
            {
                ConversationId = conversationId,
                ReaderId = userId,
                ReadAt = readAt,
                UpdatedCount = updatedCount
            };

            await PublishConversationMessagesReadSafelyAsync(conversationId, readResponse);
            await PublishConversationCardAfterReadSafelyAsync(conversation, cancellationToken);

            return Result.Success();
        }

        private ConversationListItemResponse ToConversationListItemResponse(conversation conversation, Guid currentUserId, message? latestMessage, int unreadCount)
        {
            var otherParticipant = conversation.UserOneId == currentUserId ? conversation.UserTwo : conversation.UserOne;
            var otherParticipantId = conversation.UserOneId == currentUserId ? conversation.UserTwoId : conversation.UserOneId;

            return new ConversationListItemResponse
            {
                ConversationId = conversation.ConversationId,

                OtherParticipant = new ConversationParticipantResponse
                {
                    UserId = otherParticipantId,
                    DisplayName = otherParticipant?.Username ?? string.Empty,
                    AvatarUrl = otherParticipant?.AvatarUrl
                },

                LatestNegotiationId = latestMessage?.NegotiationId,
                LatestMessageId = latestMessage?.MessageId,
                LatestMessageSenderId = latestMessage?.SenderId,
                LatestMessagePreview = latestMessage is null
                    ? null
                    : BuildConversationPreview(latestMessage),
                LatestMessageType = latestMessage?.MessageType,
                LatestMessageAt = latestMessage?.CreatedAt,
                UnreadCount = unreadCount,
                LastActivityAt = conversation.LastActivityAt,
                CreatedAt = conversation.CreatedAt
            };
        }

        private static NegotiationListItemResponse ToNegotiationListItemResponse(negotiation negotiation, Guid currentUserId, int unreadCount)
        {
            var otherPartyId = negotiation.BuyerId == currentUserId ? negotiation.SellerId : negotiation.BuyerId;
            var otherParty = negotiation.BuyerId == currentUserId ? negotiation.Seller : negotiation.Buyer;

            return new NegotiationListItemResponse
            {
                NegotiationId = negotiation.NegotiationId,
                ConversationId = negotiation.ConversationId,
                OfferId = negotiation.OfferId,
                PostId = negotiation.PostId,

                OtherPartyId = otherPartyId,
                OtherPartyName = otherParty?.Username ?? string.Empty,
                OtherPartyAvatarUrl = otherParty?.AvatarUrl,

                CurrentOfferPrice = negotiation.Offer?.OfferPrice,
                CurrentOfferQuantity =
                    negotiation.Offer?.OfferQuantity ?? 0,
                CurrentOfferVersion =
                    negotiation.Offer?.Version ?? 0,

                NegotiationStatus =
                    negotiation.NegotiationStatus
                    ?? NegotiationStatus.Open,

                LastMessageAt = negotiation.LastMessageAt,
                CreatedAt = negotiation.CreatedAt,
                UnreadCount = unreadCount
            };
        }

        private async Task PublishConversationCardAfterReadSafelyAsync(conversation conversation, CancellationToken cancellationToken)
        {
            try
            {
                var latestMessages = await _messageRepository.GetLatestByConversationsAsync(new[] { conversation.ConversationId }, cancellationToken);

                if (!latestMessages.TryGetValue(conversation.ConversationId, out var latestMessage))
                    return;

                // Hiện tại Message vẫn được gửi qua Negotiation
                if (!latestMessage.NegotiationId.HasValue)
                    return;

                var negotiation = await _negotiationRepository.GetByIdAsync(latestMessage.NegotiationId.Value, cancellationToken);

                if (negotiation is null)
                    return;

                var currentOffer = negotiation.Offer ?? await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);

                var messageResponse = _mapper.Map<MessageResponse>(latestMessage);

                await PublishConversationUpdatedSafelyAsync(
                    conversation.ConversationId,
                    negotiation.NegotiationId,
                    conversation.UserOneId,
                    conversation.UserTwoId,
                    messageResponse,
                    negotiation.NegotiationStatus
                        ?? NegotiationStatus.Open,
                    currentOffer?.OfferPrice,
                    currentOffer?.OfferQuantity ?? 0,
                    currentOffer?.Version);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể cập nhật thẻ Conversation {ConversationId} sau khi đọc.",
                    conversation.ConversationId);
            }
        }

        private async Task PublishConversationMessagesReadSafelyAsync(Guid conversationId, ConversationMessagesReadResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishConversationMessagesReadAsync(conversationId, response, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationMessagesRead cho ConversationId {ConversationId}.",
                    conversationId);
            }
        }

        private async Task PublishConversationUpdatedSafelyAsync(Guid conversationId, Guid negotiationId, Guid userOneId, Guid userTwoId, MessageResponse lastMessage, NegotiationStatus status, decimal? price, int quantity, int? version)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var unreadDetails = await _messageRepository.GetUnreadCountsDetailAsync( conversationId, userOneId, userTwoId, timeout.Token);

                var conversationUnread = unreadDetails.ToDictionary(
                    item => item.Key,
                    item => item.Value.TotalConversationUnread);

                var negotiationUnread = unreadDetails.ToDictionary(
                    item => item.Key,
                    item => item.Value.UnreadByNegotiation.ToDictionary(
                        unread => unread.Key,
                        unread => (int?)unread.Value));

                await _realtimePublisher.PublishConversationUpdatedAsync(
                    new[] { userOneId, userTwoId },
                    new ConversationUpdatedResponse
                    {
                        ConversationId = conversationId,
                        NegotiationId = negotiationId,

                        LastSenderId = lastMessage.SenderId,
                        LastMessagePreview =
                            BuildConversationPreview(lastMessage),
                        LastMessageType = lastMessage.MessageType,
                        LastMessageAt = lastMessage.CreatedAt,

                        CurrentOfferPrice = price,
                        CurrentOfferQuantity = quantity,
                        CurrentOfferVersion = version,
                        NegotiationStatus = status,

                        ConversationUnreadByUser = conversationUnread,
                        NegotiationUnreadByUser = negotiationUnread
                    },
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationUpdated cho ConversationId {ConversationId}.",
                    conversationId);
            }
        }

        private static bool IsParticipant(conversation conversation, Guid userId)
        {
            return conversation.UserOneId == userId || conversation.UserTwoId == userId;
        }

        private static string? GetPaginationError(PaginationRequest request)
        {
            if (request.PageNumber < 1)
                return "PageNumber phải lớn hơn hoặc bằng 1.";

            if (request.PageSize < 1 || request.PageSize > 100)
                return "PageSize phải nằm trong khoảng từ 1 đến 100.";

            return null;
        }

        private static string BuildConversationPreview(message message)
        {
            return message.MessageType switch
            {
                MessageType.Text => message.MessageContent ?? string.Empty,
                MessageType.Media => "[Hình ảnh]",
                MessageType.Offer or MessageType.CounterOffer => $"Đề nghị {message.OfferPrice:N0}đ x {message.OfferQuantity}",
                MessageType.System => message.MessageContent ?? "[Hệ thống]",

                _ => "[Tin nhắn]"
            };
        }

        private static string BuildConversationPreview(MessageResponse message)
        {
            return message.MessageType switch
            {
                MessageType.Text => message.MessageContent ?? string.Empty,
                MessageType.Media => "[Hình ảnh]",
                MessageType.Offer or MessageType.CounterOffer => $"Đề nghị {message.OfferPrice:N0}đ x {message.OfferQuantity}",
                MessageType.System => message.MessageContent ?? "[Hệ thống]",

                _ => "[Tin nhắn]"
            };
        }
    }
}
