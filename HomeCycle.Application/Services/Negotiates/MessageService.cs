using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Generics;
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
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IOfferRepository _offerRepository;
        private readonly IValidator<SendMessageRequest> _sendValidator;
        private readonly IChatRealtimePublisher _realtimePublisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            INegotiationRepository negotiationRepository,
            IConversationRepository conversationRepository,
            IOfferRepository offerRepository,
            IValidator<SendMessageRequest> sendValidator,
            IChatRealtimePublisher realtimePublisher,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _negotiationRepository = negotiationRepository;
            _conversationRepository = conversationRepository;
            _offerRepository = offerRepository;
            _sendValidator = sendValidator;
            _realtimePublisher = realtimePublisher;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<MessageResponse>> SendAsync(Guid userId, Guid negotiationId, SendMessageRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _sendValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(
                    ", ",
                    validationResult.Errors.Select(x => x.ErrorMessage));

                return Result<MessageResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var normalizedContent = request.MessageContent.Trim();

            MessageResponse createdResponse;

            Guid conversationId = Guid.Empty;
            Guid negotiationSellerId = Guid.Empty;
            Guid negotiationBuyerId = Guid.Empty;
            NegotiationStatus negotiationStatus = NegotiationStatus.Open;

            decimal? negotiationOfferPrice = null;
            int negotiationOfferQuantity = 0;
            int? negotiationOfferVersion = null;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // tuần tự hóa các thao tác gửi trên cùng một phiên negotiation - tránh race condition
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync( negotiationId, cancellationToken);
                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<MessageResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!NegotiationAccess.IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<MessageResponse>.Fail(NegotiationErrors.Forbidden);
                }

                conversationId = RequireConversationId(negotiation);

                var currentOffer = negotiation.Offer;
                if (currentOffer is null)
                {
                    currentOffer = await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);
                }

                if (currentOffer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<MessageResponse>.Fail(OfferErrors.NotFound);
                }

                negotiationSellerId = negotiation.SellerId;
                negotiationBuyerId = negotiation.BuyerId;
                negotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open;
                //negotiationOfferPrice = negotiation.Offer?.OfferPrice;
                //negotiationOfferQuantity = negotiation.Offer?.OfferQuantity ?? 0;
                //negotiationOfferVersion = negotiation.Offer?.Version;

                negotiationOfferPrice = currentOffer.OfferPrice;
                negotiationOfferQuantity = currentOffer.OfferQuantity;
                negotiationOfferVersion = currentOffer.Version;

                var existingMessage =
                    await _messageRepository.GetByClientMessageIdAsync(
                        negotiationId,
                        userId,
                        request.ClientMessageId,
                        cancellationToken);

                if (existingMessage is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    var isSameRequest =
                        existingMessage.MessageType == MessageType.Text &&
                        string.Equals(
                            existingMessage.MessageContent,
                            normalizedContent,
                            StringComparison.Ordinal);

                    if (!isSameRequest)
                    {
                        return Result<MessageResponse>.Fail(MessageErrors.ClientMessageIdConflict);
                    }

                    return Result<MessageResponse>.Success( _mapper.Map<MessageResponse>(existingMessage));
                }

                if (!CanSendTextMessage(negotiationStatus))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<MessageResponse>.Fail(MessageErrors.NegotiationReadOnly);
                }

                var now = DateTime.UtcNow;

                var newMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiationId,
                    ConversationId = conversationId,
                    SenderId = userId,
                    ClientMessageId = request.ClientMessageId,

                    MessageType = MessageType.Text,
                    MessageContent = normalizedContent,

                    OfferPrice = null,
                    OfferQuantity = 0,
                    OfferStatus = null,
                    BasePriceSnapshot = null,
                    MediaUrl = null,

                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                negotiation.LastMessageAt = now;

                await _messageRepository.AddAsync(newMessage, cancellationToken);
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversationId, now, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                createdResponse =
                    _mapper.Map<MessageResponse>(newMessage);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            await PublishMessageCreatedSafelyAsync(negotiationId, createdResponse);
            await PublishConversationMessageCreatedSafelyAsync(conversationId, createdResponse);

            // cập nhật thẻ chat ngoài list cho cả 2 bên
            await PublishConversationUpdatedSafelyAsync(
                conversationId,
                negotiationId,
                negotiationSellerId,
                negotiationBuyerId,
                createdResponse,
                negotiationStatus,
                negotiationOfferPrice,
                negotiationOfferQuantity,
                negotiationOfferVersion);

            return Result<MessageResponse>.Success(createdResponse);
        }

        public async Task<Result<PagedResult<MessageResponse>>> GetHistoryAsync(Guid userId, Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            if (request.PageNumber < 1)
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    ValidationErrors.InvalidRequest("PageNumber phải lớn hơn hoặc bằng 1."));
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    ValidationErrors.InvalidRequest("PageSize phải nằm trong khoảng từ 1 đến 100."));
            }

            var negotiation =
                await _negotiationRepository.GetByIdAsync(
                    negotiationId,
                    cancellationToken);

            if (negotiation is null)
                return Result<PagedResult<MessageResponse>>.Fail(NegotiationErrors.NotFound);

            if (!NegotiationAccess.IsParticipant(negotiation, userId))
                return Result<PagedResult<MessageResponse>>.Fail(NegotiationErrors.Forbidden);

            var pagedMessages =
                await _messageRepository.GetByNegotiationIdAsync(
                    negotiationId,
                    request,
                    cancellationToken);

            var response = new PagedResult<MessageResponse>
                {
                    Items = pagedMessages.Items
                        .Select(x => _mapper.Map<MessageResponse>(x))
                        .ToList(),

                    PageNumber = pagedMessages.PageNumber,
                    PageSize = pagedMessages.PageSize,
                    TotalCount = pagedMessages.TotalCount
                };

            return Result<PagedResult<MessageResponse>>.Success(response);
        }

        public async Task<Result> MarkAsReadAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);

            if (negotiation is null)
                return Result.Fail(NegotiationErrors.NotFound);

            if (!NegotiationAccess.IsParticipant(negotiation, userId))
                return Result.Fail(NegotiationErrors.Forbidden);

            var conversationId = RequireConversationId(negotiation);
            var readAt = DateTime.UtcNow;

            var updatedCount =
                await _messageRepository.MarkAsReadAsync(
                    negotiationId,
                    userId,
                    readAt,
                    cancellationToken);

            // Không có tin chưa đọc vẫn được xem là thành công.
            // Điều này giúp endpoint có tính idempotent.
            if (updatedCount == 0)
                return Result.Success();

            var readResponse = new MessagesReadResponse
            {
                NegotiationId = negotiationId,
                ReaderId = userId,
                ReadAt = readAt,
                UpdatedCount = updatedCount
            };

            await PublishMessagesReadSafelyAsync(negotiationId, readResponse);

            // cập nhật unread trên thẻ chat ngoài list cho cả 2 bên
            //var lastMessages = await _messageRepository.GetByNegotiationIdAsync(negotiationId, new PaginationRequest { PageNumber = 1, PageSize = 1 }, cancellationToken);

            //Preview ngoài Inbox luôn là tin nhắn mới nhất tuyệt đối của cuộc trò chuyện, bất kể hành động đọc diễn ra ở Negotiation nào
            var lastMessages =
                await _messageRepository.GetByConversationIdAsync(
                    conversationId,
                    new PaginationRequest
                    {
                        PageNumber = 1,
                        PageSize = 1
                    }, cancellationToken);

            var currentOffer = negotiation.Offer ?? await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);

            var lastMessage = lastMessages.Items.FirstOrDefault();
            if (lastMessage is not null)
            {
                await PublishConversationUpdatedSafelyAsync(
                    conversationId, 
                    negotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(lastMessage),
                    negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                    //negotiation.Offer?.OfferPrice,
                    //negotiation.Offer?.OfferQuantity ?? 0,
                    //negotiation.Offer?.Version
                    currentOffer?.OfferPrice,
                    currentOffer?.OfferQuantity ?? 0,
                    currentOffer?.Version);
            }

            return Result.Success();
        }

        public async Task<Result> MarkConversationAsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
            if (conversation is null)
                return Result.Fail(NegotiationErrors.NotFound);

            var isParticipant = conversation.UserOneId == userId || conversation.UserTwoId == userId;
            if (!isParticipant)
                return Result.Fail(NegotiationErrors.Forbidden);

            var readAt = DateTime.UtcNow;

            var updatedCount =
                await _messageRepository.MarkConversationAsReadAsync(
                    conversationId,
                    userId,
                    readAt,
                    cancellationToken);

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

            var lastMessages = await _messageRepository.GetByConversationIdAsync(conversationId, new PaginationRequest { PageNumber = 1, PageSize = 1 }, cancellationToken);
            var lastMessage = lastMessages.Items.FirstOrDefault();

            if (lastMessage is not null)
            {
                var negotiation = await _negotiationRepository.GetByIdAsync(lastMessage.NegotiationId ?? Guid.Empty, cancellationToken);
                if (negotiation is not null)
                {
                    var currentOffer = negotiation.Offer ?? await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);
                    await PublishConversationUpdatedSafelyAsync(
                        conversationId,
                        negotiation.NegotiationId,
                        negotiation.SellerId,
                        negotiation.BuyerId,
                        _mapper.Map<MessageResponse>(lastMessage),
                        negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                        currentOffer?.OfferPrice,
                        currentOffer?.OfferQuantity ?? 0,
                        currentOffer?.Version);
                }
            }

            return Result.Success();
        }

        private static bool CanSendTextMessage(NegotiationStatus status)
        {
            return status is NegotiationStatus.Open or NegotiationStatus.Agreed;
        }

        private async Task PublishMessageCreatedSafelyAsync(Guid negotiationId, MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishMessageCreatedAsync(
                    negotiationId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát MessageCreated cho MessageId {MessageId}. " +
                    "Tin nhắn đã được lưu vào database.",
                    response.MessageId);
            }
        }

        private async Task PublishMessagesReadSafelyAsync(Guid negotiationId, 
            MessagesReadResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishMessagesReadAsync(
                    negotiationId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát MessagesRead cho NegotiationId " +
                    "{NegotiationId}. Trạng thái đã đọc đã được lưu.",
                    negotiationId);
            }
        }

        private async Task PublishConversationMessagesReadSafelyAsync(Guid conversationId,
            ConversationMessagesReadResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishConversationMessagesReadAsync(
                    conversationId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationMessagesRead cho ConversationId " +
                    "{ConversationId}. Trạng thái đã đọc đã được lưu.",
                    conversationId);
            }
        }

        private async Task PublishConversationUpdatedSafelyAsync(
            Guid conversationId, Guid negotiationId, Guid sellerId, Guid buyerId,
            MessageResponse lastMessage, NegotiationStatus status,
            decimal? price, int quantity, int? version)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var unreadDetails = await _messageRepository.GetUnreadCountsDetailAsync(
                    conversationId, sellerId, buyerId, timeout.Token);

                var conversationUnread = unreadDetails.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.TotalConversationUnread);

                var negotiationUnread = unreadDetails.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.UnreadByNegotiation.ToDictionary(
                        innerKv => innerKv.Key,
                        innerKv => (int?)innerKv.Value));

                await _realtimePublisher.PublishConversationUpdatedAsync(
                    new[] { sellerId, buyerId },
                    new ConversationUpdatedResponse
                    {
                        ConversationId = conversationId,
                        NegotiationId = negotiationId,
                        LastSenderId = lastMessage.SenderId,
                        LastMessagePreview = BuildConversationPreview(lastMessage),
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
                    "Không thể phát ConversationUpdated cho NegotiationId {NegotiationId}.",
                    negotiationId);
            }
        }

        private async Task PublishConversationMessageCreatedSafelyAsync(Guid conversationId, MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _realtimePublisher.PublishConversationMessageCreatedAsync(conversationId, response, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationMessageCreated " +
                    "cho MessageId {MessageId}.",
                    response.MessageId);
            }
        }

        private static string BuildConversationPreview(MessageResponse m)
        {
            return m.MessageType switch
            {
                MessageType.Text => m.MessageContent ?? string.Empty,
                MessageType.Media => "[Hình ảnh]",
                MessageType.Offer or MessageType.CounterOffer =>
                    $"Đề nghị {m.OfferPrice:N0}đ x {m.OfferQuantity}",
                _ => "[Hệ thống]"
            };
        }

        private static Guid RequireConversationId(negotiation negotiation)
        {
            return negotiation.ConversationId
                ?? throw new InvalidOperationException(
                    $"Negotiation {negotiation.NegotiationId} " +
                    "chưa được liên kết với Conversation.");
        }
    }
}
