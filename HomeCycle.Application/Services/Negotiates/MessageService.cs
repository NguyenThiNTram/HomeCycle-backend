using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
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
        private readonly IValidator<SendMessageRequest> _sendValidator;
        private readonly IChatRealtimePublisher _realtimePublisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            INegotiationRepository negotiationRepository,
            IValidator<SendMessageRequest> sendValidator,
            IChatRealtimePublisher realtimePublisher,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _negotiationRepository = negotiationRepository;
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

            Guid negotiationSellerId = Guid.Empty;
            Guid negotiationBuyerId = Guid.Empty;
            NegotiationStatus negotiationStatus = NegotiationStatus.Open;
            decimal? negotiationOfferPrice = null;
            int negotiationOfferQuantity = 0;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa negotiation để tuần tự hóa các thao tác gửi
                // trên cùng một phiên thương lượng.
                var negotiation =
                    await _negotiationRepository.GetByIdForUpdateAsync(
                        negotiationId,
                        cancellationToken);

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

                negotiationSellerId = negotiation.SellerId;
                negotiationBuyerId = negotiation.BuyerId;
                negotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open;
                negotiationOfferPrice = negotiation.Offer?.OfferPrice;
                negotiationOfferQuantity = negotiation.Offer?.OfferQuantity ?? 0;

                /*
                 * Kiểm tra chống gửi trùng.
                 *
                 * Nếu FE retry cùng ClientMessageId và cùng nội dung,
                 * coi như request đã thành công trước đó.
                 */
                var existingMessage =
                    await _messageRepository.GetByClientMessageIdAsync(
                        negotiationId,
                        userId,
                        request.ClientMessageId,
                        cancellationToken);

                if (existingMessage is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    var isSameRequest =
                        existingMessage.MessageType == MessageType.Text &&
                        string.Equals(
                            existingMessage.MessageContent,
                            normalizedContent,
                            StringComparison.Ordinal);

                    if (!isSameRequest)
                    {
                        return Result<MessageResponse>.Fail(
                            MessageErrors.ClientMessageIdConflict);
                    }

                    return Result<MessageResponse>.Success(
                        _mapper.Map<MessageResponse>(existingMessage));
                }

                if (!CanSendTextMessage((NegotiationStatus)negotiation.NegotiationStatus))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<MessageResponse>.Fail(MessageErrors.NegotiationReadOnly);
                }

                var now = DateTime.UtcNow;

                var newMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiationId,
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

            // SignalR được gọi sau khi transaction đã commit.
            await PublishMessageCreatedSafelyAsync(
                negotiationId,
                createdResponse);

            // Realtime: cập nhật thẻ chat ngoài list cho cả 2 bên.
            await PublishConversationUpdatedSafelyAsync(
                negotiationId,
                negotiationSellerId,
                negotiationBuyerId,
                createdResponse,
                negotiationStatus,
                negotiationOfferPrice,
                negotiationOfferQuantity);

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
            var negotiation =
                await _negotiationRepository.GetByIdAsync(
                    negotiationId,
                    cancellationToken);

            if (negotiation is null)
                return Result.Fail(NegotiationErrors.NotFound);

            if (!NegotiationAccess.IsParticipant(negotiation, userId))
                return Result.Fail(NegotiationErrors.Forbidden);

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

            // Realtime: cập nhật badge unread trên thẻ chat ngoài list cho cả 2 bên.
            var lastMessages = await _messageRepository.GetByNegotiationIdAsync(
                negotiationId,
                new PaginationRequest { PageNumber = 1, PageSize = 1 },
                cancellationToken);

            var lastMessage = lastMessages.Items.FirstOrDefault();
            if (lastMessage is not null)
            {
                await PublishConversationUpdatedSafelyAsync(
                    negotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(lastMessage),
                    negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                    negotiation.Offer?.OfferPrice,
                    negotiation.Offer?.OfferQuantity ?? 0);
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

        private async Task PublishConversationUpdatedSafelyAsync(
            Guid negotiationId, Guid sellerId, Guid buyerId,
            MessageResponse lastMessage, NegotiationStatus status,
            decimal? price, int quantity)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var unread = await _messageRepository.GetUnreadCountsByNegotiationAsync(
                    negotiationId, buyerId, sellerId, timeout.Token);

                await _realtimePublisher.PublishConversationUpdatedAsync(
                    new[] { sellerId, buyerId },
                    new ConversationUpdatedResponse
                    {
                        NegotiationId = negotiationId,
                        LastSenderId = lastMessage.SenderId,
                        LastMessagePreview = BuildConversationPreview(lastMessage),
                        LastMessageType = lastMessage.MessageType,
                        LastMessageAt = lastMessage.CreatedAt,
                        CurrentOfferPrice = price,
                        CurrentOfferQuantity = quantity,
                        NegotiationStatus = status,
                        UnreadCountByUser = unread
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
    }
}
