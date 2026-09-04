using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Offers
{
    public class OfferService : IOfferService
    {
        private readonly IOfferRepository _offerRepository;
        private readonly IOfferTermsPolicy _offerTermsPolicy;
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OfferService> _logger;
        private readonly IValidator<CreateOfferRequest> _createValidator;
        private readonly IValidator<UpdateOfferRequest> _updateValidator;
        private readonly IValidator<CounterInitialOfferRequest> _counterInitialValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRealtimePublisher _realtimePublisher;
        private readonly INotificationService _notificationService;

        private const decimal MinPriceFactor = 0.2m;
        private const decimal MaxPriceFactor = 3m;

        public OfferService(
            IOfferRepository offerRepository,
            IOfferTermsPolicy offerTermsPolicy,
            INegotiationRepository negotiationRepository,
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ILogger<OfferService> logger,
            IValidator<CreateOfferRequest> createValidator,
            IValidator<UpdateOfferRequest> updateValidator,
            IValidator<CounterInitialOfferRequest> counterInitialValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IChatRealtimePublisher realtimePublisher,
            INotificationService notificationService)
        {
            _offerRepository = offerRepository;
            _offerTermsPolicy = offerTermsPolicy;
            _negotiationRepository = negotiationRepository;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _logger = logger;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _counterInitialValidator = counterInitialValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _realtimePublisher = realtimePublisher;
            _notificationService = notificationService;
        }

        // ================== GIAI ĐOẠN 1: NGOÀI NEGOTIATION ==================

        public async Task<Result<OfferResponse>> CreateAsync(Guid userId, CreateOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<OfferResponse>.Fail(ToValidationError(validation));

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post is null)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);

            if (post.Status != PostStatus.Active)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);

            if (post.OwnerId == userId)
                return Result<OfferResponse>.Fail(OfferErrors.CannotOfferOwnPost);

            var roleError = await ValidateOfferRoleAsync(post, userId, cancellationToken);
            if (roleError is not null)
                return Result<OfferResponse>.Fail(roleError);

            if (request.OfferQuantity > post.RemainingQuantity)
                return Result<OfferResponse>.Fail(
                    OfferErrors.QuantityExceedsRemaining(
                        request.OfferQuantity,
                        post.RemainingQuantity));

            var expectedBaseTotalPrice = post.BasePrice * request.OfferQuantity;

            var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice);
            if (priceError is not null)
                return Result<OfferResponse>.Fail(priceError);

            var hasPendingOffer =
                await _offerRepository.ExistsPendingByPostAndSenderAsync(
                    request.PostId,
                    userId,
                    cancellationToken);

            if (hasPendingOffer)
                return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending);

            var offer = _mapper.Map<offer>(request);

            offer.OfferId = Guid.NewGuid();
            offer.PostId = post.PostId;
            offer.SenderId = userId;
            offer.ReceiverId = post.OwnerId;
            offer.OfferStatus = OfferStatus.Pending;
            offer.Version = 1; //thêm version để check update
            offer.CreatedAt = DateTime.UtcNow;

            notification createdNotification = null!;

            try
            {
                await _offerRepository.AddAsync(offer, cancellationToken);
                _logger.LogInformation("Creating offer for PostId: {PostId}", offer.PostId);

                createdNotification =
                    await AddOfferNotificationPendingAsync(
                        offer,
                        userId,
                        "Bạn có đề nghị mới",
                        "Bạn vừa nhận được một đề nghị thương lượng mới.",
                        cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
            }
            
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending);
            }

            var created = await _offerRepository.GetByIdAsync(offer.OfferId, cancellationToken);
            if (created is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);
            
            await PublishOfferCreatedSafelyAsync(created);
            await _notificationService.PublishCreatedSafelyAsync(createdNotification);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(created));
        }

        // Người gửi chỉ được sửa giá và số lượng khi request ban đầu còn Pending
        public async Task<Result<OfferResponse>> UpdateAsync(Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<OfferResponse>.Fail(ToValidationError(validation));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            offer? updatedOffer = null;
            notification updatedNotification = null!;
            try
            {
                // Khóa Offer để Update không chạy đồng thời với Accept
                //var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
                var offer = await _offerRepository.GetByIdForUpdateAsync(offerId, cancellationToken);
                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotFound);
                }

                if (offer.SenderId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.Forbidden);
                }

                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotPending);
                }

                // Người gửi đang cập nhật version cũ
                if (offer.Version != request.Version)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<OfferResponse>.Fail(
                        OfferErrors.OfferTermsChanged(
                            offer.OfferPrice!.Value,
                            offer.OfferQuantity));
                }

                var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status != PostStatus.Active)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);
                }

                var newPrice = request.OfferPrice!.Value;
                var newQuantity = request.OfferQuantity!.Value;

                if (newQuantity > post.RemainingQuantity)
                {
                     await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                     return Result<OfferResponse>.Fail(
                         OfferErrors.QuantityExceedsRemaining(newQuantity, post.RemainingQuantity));
                }

                var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice.Value);
                if (priceError is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(priceError);
                }

                //offer.OfferPrice = request.OfferPrice.Value;
                //offer.OfferQuantity = request.OfferQuantity.Value;

                //_mapper.Map(request, offer);
                //offer.Version++;

                var termsChanged = offer.OfferPrice != newPrice || offer.OfferQuantity != newQuantity;

                if (!termsChanged)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<OfferResponse>.Success(
                        _mapper.Map<OfferResponse>(offer));
                }

                offer.OfferPrice = newPrice;
                offer.OfferQuantity = newQuantity;
                offer.Version = (offer.Version ?? 1) + 1;

                updatedNotification =
                    await AddOfferNotificationPendingAsync(
                        offer,
                        userId,
                        "Đề nghị đã được cập nhật",
                        "Người gửi đã cập nhật giá hoặc số lượng của đề nghị.",
                        cancellationToken);

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                //await PublishOfferUpdatedSafelyAsync(offer);
                updatedOffer = offer;
                
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
            // Chỉ realtime sau khi transaction đã commit success
            await PublishOfferUpdatedSafelyAsync(updatedOffer);
            await _notificationService.PublishCreatedSafelyAsync(updatedNotification);

            return Result<OfferResponse>.Success( _mapper.Map<OfferResponse>(updatedOffer));
        }

        // Người gửi tự hủy request && hủy Offer ban đầu khi còn Pending
        public async Task<Result<OfferResponse>> CancelAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            offer cancelledOffer = null!;
            notification cancelledNotification = null!;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotFound);
                }

                if (offer.SenderId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.Forbidden);
                }

                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotPending);
                }

                offer.OfferStatus = OfferStatus.Cancelled;

                cancelledNotification = await AddOfferNotificationPendingAsync(
                    offer, userId,
                    "Đề nghị đã được hủy",
                    "Người gửi đã hủy đề nghị thương lượng.",
                    cancellationToken);

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                //await PublishOfferUpdatedSafelyAsync(offer);
                cancelledOffer = offer;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var cancelOffer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);

            await PublishOfferUpdatedSafelyAsync(cancelOffer!);
            await _notificationService.PublishCreatedSafelyAsync(cancelledNotification);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(cancelOffer));
        }

        // Người nhận từ chối request ban đầu
        public async Task<Result<OfferResponse>> RejectAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            offer rejectedOffer = null!;
            notification rejectedNotification = null!;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotFound);
                }

                if (offer.ReceiverId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.Forbidden);
                }

                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<OfferResponse>.Fail(OfferErrors.NotPending);
                }

                offer.OfferStatus = OfferStatus.Rejected;

                // Người nhận từ chối -> thông báo ngược lại cho người gửi.
                rejectedNotification =
                    await AddOfferNotificationPendingAsync(
                        offer,
                        userId,
                        "Đề nghị đã bị từ chối",
                        "Người nhận đã từ chối đề nghị thương lượng của bạn.",
                        cancellationToken);

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                //await PublishOfferUpdatedSafelyAsync(offer);
                rejectedOffer = offer;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var rejectOffer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);

            await PublishOfferUpdatedSafelyAsync(rejectOffer!);
            await _notificationService.PublishCreatedSafelyAsync(rejectedNotification);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(rejectOffer));
        }

        // Accept bên ngoài chỉ chấp nhận mở phiên thương lượng
        // Tạo Negotiation Agreed && Chưa chốt, chưa trừ kho và chưa tạo AgreementForm
        public async Task<Result<AcceptOfferResponse>> AcceptAsync(Guid userId, Guid offerId, AcceptOfferRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var offer = await _offerRepository.GetByIdForUpdateAsync(offerId, cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.NotFound);
                }

                // Chỉ người nhận/chủ bài đăng được phản hồi Offer ban đầu.
                if (offer.ReceiverId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.Forbidden);
                }

                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.NotPending);
                }

                if (offer.Version != request.Version)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(
                        OfferErrors.OfferTermsChanged(
                            offer.OfferPrice!.Value,
                            offer.OfferQuantity));
                }

                var post = await _postRepository.GetByIdAsync(
                    offer.PostId,
                    cancellationToken);

                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status != PostStatus.Active)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.PostNotActive);
                }

                // Dùng policy chung cho Create/Update/Accept/Counter.
                var termsError = _offerTermsPolicy.Validate(
                    post,
                    (decimal)offer.OfferPrice,
                    offer.OfferQuantity);

                if (termsError is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(termsError);
                }

                var now = DateTime.UtcNow;

                // Hai bên đã đồng ý nguyên điều khoản của Offer.
                var negotiation = CreateAgreedNegotiation(offer, post, now);

                var hasActiveNegotiation = await _negotiationRepository.ExistsActiveByPostAndParticipantsAsync(negotiation.PostId, negotiation.SellerId, negotiation.BuyerId, cancellationToken);

                if (hasActiveNegotiation)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(NegotiationErrors.AlreadyExists);
                }

                var conversation = await _conversationRepository.GetOrCreateAsync(negotiation.SellerId, negotiation.BuyerId, now, cancellationToken);

                negotiation.ConversationId = conversation.ConversationId;

                var initialOfferMessage = CreateInitialOfferMessage(offer, conversation.ConversationId, negotiation.NegotiationId, post.BasePrice, MessageOfferStatus.Accepted, now);

                // Offer Accepted nghĩa là Offer đã được xử lý và đưa vào Negotiation.
                offer.OfferStatus = OfferStatus.Accepted;

                var acceptedNotification =
                    await AddOfferNotificationPendingAsync(
                        offer,
                        userId,
                        "Đề nghị đã được chấp nhận",
                        "Người nhận đã chấp nhận đề nghị của bạn.",
                        cancellationToken);

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _negotiationRepository.AddAsync(negotiation, cancellationToken);
                await _messageRepository.AddAsync(initialOfferMessage, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversation.ConversationId, now, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Realtime: đẩy message mở negotiation
                await PublishMessageCreatedSafelyAsync(
                    negotiation.NegotiationId,
                    _mapper.Map<MessageResponse>(initialOfferMessage));

                // Realtime: hiện hộp chat Nego mới mở cho cả 2 bên (Buyer + Seller)
                await PublishConversationUpdatedSafelyAsync(
                    negotiation.NegotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(initialOfferMessage),
                    NegotiationStatus.Agreed,
                    offer.OfferPrice,
                    offer.OfferQuantity);

                // Realtime: đẩy OfferUpdated cho cả 2 bên (Sender + Receiver) để cập nhật trạng thái Accepted
                await PublishOfferUpdatedSafelyAsync(offer);
                await _notificationService.PublishCreatedSafelyAsync(acceptedNotification);

                return Result<AcceptOfferResponse>.Success(
                    new AcceptOfferResponse
                    {
                        OfferId = offer.OfferId,
                        NegotiationId = negotiation.NegotiationId,
                        ConversationId = conversation.ConversationId,
                        OfferStatus = OfferStatus.Accepted,
                        NegotiationStatus = NegotiationStatus.Agreed
                    });
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<AcceptOfferResponse>.Fail(NegotiationErrors.AlreadyExists);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // Người nhận counter request ban đầu -> mở Negotiation, lưu proposal gốc -> thành Superseded và lưu counter mới thành Pending && Tạo Negotiation ở trạng thái Open
        public async Task<Result<NegotiationResponse>> CounterInitialOfferAsync(Guid userId, Guid offerId, CounterInitialOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _counterInitialValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<NegotiationResponse>.Fail(ToValidationError(validation));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa dòng Offer TRONG transaction: chống Accept + Counter && một Offer tạo 2 Negotiation
                var offer = await _offerRepository.GetByIdForUpdateAsync(offerId, cancellationToken);
                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotFound);
                }

                if (offer.ReceiverId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotPending);
                }
                if (offer.Version != request.Version)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(
                        OfferErrors.OfferTermsChanged(offer.OfferPrice!.Value, offer.OfferQuantity));
                }

                // Check 1 OfferId - 1 Negotiation
                var existingNegotiation = await _negotiationRepository.GetByOfferIdAsync(offerId, cancellationToken);
                if (existingNegotiation is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.AlreadyExists);
                }

                var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status != PostStatus.Active)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.PostNotActive);
                }

                if (request.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            request.OfferQuantity,
                            post.RemainingQuantity));
                }

                var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice);
                if (priceError is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(priceError);
                }

                var now = DateTime.UtcNow;
                var negotiation = CreateOpenNegotiation(offer, post, now);

                var hasActiveNegotiation = await _negotiationRepository.ExistsActiveByPostAndParticipantsAsync(negotiation.PostId, negotiation.SellerId,  negotiation.BuyerId, cancellationToken);

                if (hasActiveNegotiation)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.AlreadyExists);
                }

                var conversation = await _conversationRepository.GetOrCreateAsync(negotiation.SellerId, negotiation.BuyerId, now, cancellationToken);

                negotiation.ConversationId = conversation.ConversationId;

                // Giữ lịch sử request gốc
                var initialOfferMessage = CreateInitialOfferMessage(offer, conversation.ConversationId, negotiation.NegotiationId, post.BasePrice, MessageOfferStatus.Superseded, now);

                var counterMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiation.NegotiationId,
                    ConversationId = conversation.ConversationId,
                    SenderId = userId,
                    MessageType = MessageType.CounterOffer,
                    OfferPrice = request.OfferPrice,
                    OfferQuantity = request.OfferQuantity,
                    OfferStatus = MessageOfferStatus.Pending,
                    MediaUrl = null,
                    BasePriceSnapshot = post.BasePrice,
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                // snapshot mới + Messages giữ lịch sử thay đổi
                offer.OfferPrice = request.OfferPrice;
                offer.OfferQuantity = request.OfferQuantity;
                offer.OfferStatus = OfferStatus.Accepted;
                //offer.Version++;
                offer.Version = (offer.Version ?? 1) + 1;

                var counterNotification =
                    await AddOfferNotificationPendingAsync(
                        offer,
                        userId,
                        "Bạn nhận được đề nghị đối ứng",
                        "Người nhận đã gửi lại một đề nghị với giá hoặc số lượng mới.",
                        cancellationToken);

                // Gắn Offer vào negotiation để response counter hiện tại
                negotiation.Offer = offer;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _negotiationRepository.AddAsync(negotiation, cancellationToken);
                await _messageRepository.AddAsync(initialOfferMessage, cancellationToken);
                await _messageRepository.AddAsync(counterMessage, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversation.ConversationId, now, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _notificationService.PublishCreatedSafelyAsync(counterNotification);

                // Realtime: đẩy lịch sử offer gốc + counter mới cho group negotiation
                await PublishMessageCreatedSafelyAsync(
                    negotiation.NegotiationId,
                    _mapper.Map<MessageResponse>(initialOfferMessage));

                await PublishMessageCreatedSafelyAsync(
                    negotiation.NegotiationId,
                    _mapper.Map<MessageResponse>(counterMessage));

                // Realtime: hiện hộp chat Nego mới mở cho cả 2 bên (Buyer + Seller)
                await PublishConversationUpdatedSafelyAsync(
                    negotiation.NegotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(counterMessage),
                    NegotiationStatus.Open,
                    counterMessage.OfferPrice,
                    counterMessage.OfferQuantity);

                // Realtime: đẩy OfferUpdated cho cả 2 bên (Sender + Receiver) để cập nhật trạng thái Accepted
                await PublishOfferUpdatedSafelyAsync(offer);

                return Result<NegotiationResponse>.Success(
                    _mapper.Map<NegotiationResponse>(negotiation));
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<NegotiationResponse>.Fail(NegotiationErrors.AlreadyExists);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ================== QUERY ==================

        public async Task<Result<OfferDetailResponse>> GetByIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferDetailResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId && offer.ReceiverId != userId)
                return Result<OfferDetailResponse>.Fail(OfferErrors.Forbidden);

            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offerId, cancellationToken);

            var response = new OfferDetailResponse
            {
                OfferId = offer.OfferId,
                PostId = offer.PostId,
                OfferPrice = offer.OfferPrice,
                OfferQuantity = offer.OfferQuantity,
                OfferStatus = offer.OfferStatus ?? OfferStatus.Pending,
                NegotiationId = negotiation?.NegotiationId,
                CreatedAt = offer.CreatedAt,
                Sender = MapParticipant(offer.Sender, offer.SenderId),
                Receiver = MapParticipant(offer.Receiver, offer.ReceiverId),
                CanUpdate = offer.SenderId == userId && offer.OfferStatus == OfferStatus.Pending,
                CanCancel = offer.SenderId == userId && offer.OfferStatus == OfferStatus.Pending,
                CanAccept = offer.ReceiverId == userId && offer.OfferStatus == OfferStatus.Pending,
                CanReject = offer.ReceiverId == userId && offer.OfferStatus == OfferStatus.Pending
            };

            return Result<OfferDetailResponse>.Success(response);
        }

        public async Task<Result<PagedResult<OfferListItem>>> GetSentAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetSentAsync(
                userId,
                request,
                cancellationToken);

            return Result<PagedResult<OfferListItem>>.Success(MapPaged<offer, OfferListItem>(paged));
        }

        public async Task<Result<PagedResult<OfferListItem>>> GetReceivedAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetReceivedAsync(
                userId,
                request,
                cancellationToken);

            return Result<PagedResult<OfferListItem>>.Success(MapPaged<offer, OfferListItem>(paged));
        }

        // ================== PRIVATE HELPERS ==================

        private negotiation CreateOpenNegotiation(offer offer, post post, DateTime now)
        {
            var negotiation = _mapper.Map<negotiation>(offer);

            negotiation.NegotiationId = Guid.NewGuid();
            negotiation.FinalPrice = null;
            negotiation.FinalQuantity = null;
            negotiation.LastMessageAt = now;
            negotiation.CreatedAt = now;
            negotiation.NegotiationStatus = NegotiationStatus.Open;

            // Bài đăng Bán (Sell): chủ bài = Seller, người Offer = Buyer.
            // Bài đăng Mua (Buy): chủ bài = Buyer, người Offer = Seller.
            if (post.PostType == PostType.Buy)
            {
                negotiation.SellerId = offer.SenderId;
                negotiation.BuyerId = offer.ReceiverId;
            }
            else
            {
                negotiation.SellerId = offer.ReceiverId;
                negotiation.BuyerId = offer.SenderId;
            }

            return negotiation;
        }

        private message CreateInitialOfferMessage(offer offer, Guid conversationId, Guid negotiationId, decimal? basePrice, MessageOfferStatus status, DateTime now)
        {
            var initialMessage = _mapper.Map<message>(offer);

            initialMessage.MessageId = Guid.NewGuid();
            initialMessage.NegotiationId = negotiationId;
            initialMessage.ConversationId = conversationId;
            initialMessage.MessageContent = "Đề nghị thương lượng ban đầu";
            initialMessage.MessageType = MessageType.Offer;
            initialMessage.OfferStatus = status;
            initialMessage.MediaUrl = null;
            initialMessage.IsRead = true;
            initialMessage.BasePriceSnapshot = basePrice;
            initialMessage.CreatedAt = now;
            initialMessage.UpdatedAt = now;

            return initialMessage;
        }

        private Error? ValidatePriceRange(decimal? expectedTotalPrice, decimal offerPrice)
        {
            if (!expectedTotalPrice.HasValue)
                return OfferErrors.PriceOutOfRange(0, 0);

            var minPrice = expectedTotalPrice.Value * MinPriceFactor;
            var maxPrice = expectedTotalPrice.Value * MaxPriceFactor;

            return offerPrice < minPrice || offerPrice > maxPrice
                ? OfferErrors.PriceOutOfRange(minPrice, maxPrice)
                : null;
        }

        private async Task<Error?> ValidateOfferRoleAsync(post post, Guid userId, CancellationToken cancellationToken)
        {
            var sender = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (sender is null)
                return OfferErrors.RoleNotAllowed;
            if (sender.Status != UserStatus.Active)
                return OfferErrors.UserNotActive;

            // Personal tạo Sell Post:
            // Personal hoặc Business có thể gửi Offer.
            if (post.PostType == PostType.Sell)
            {
                return sender.Role is UserRole.Personal or UserRole.Business
                    ? null
                    : OfferErrors.RoleNotAllowed;
            }

            // Business tạo Buy Post:
            // chỉ Personal được gửi Offer.
            if (post.PostType == PostType.Buy)
            {
                return sender.Role == UserRole.Personal
                    ? null
                    : OfferErrors.BusinessCannotOfferBuyPost;
            }

            return null;
        }

        private static OfferParticipantResponse MapParticipant(user? participant, Guid userId)
        {
            return new OfferParticipantResponse
            {
                UserId = userId,
                DisplayName = participant?.Username ?? string.Empty,
                AvatarUrl = participant?.AvatarUrl
            };
        }

        private PagedResult<TDestination> MapPaged<TSource, TDestination>(PagedResult<TSource> source)
        {
            return new PagedResult<TDestination>
            {
                Items = _mapper.Map<IReadOnlyList<TDestination>>(source.Items),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            };
        }

        private static Error ToValidationError(FluentValidation.Results.ValidationResult validation)
        {
            var errors = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
            return ValidationErrors.InvalidRequest(errors);
        }

        // Nhận diện lỗi vi phạm UNIQUE constraint của Postgres (SQLSTATE 23505) trong chuỗi InnerException.
        // Dùng reflection thay vì phụ thuộc trực tiếp vào Npgsql ở tầng Application.
        private static bool IsUniqueViolation(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                var sqlState = current.GetType()
                    .GetProperty("SqlState", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(current) as string;

                if (sqlState == "23505")
                    return true;
            }

            return false;
        }

        // ================== REALTIME HELPERS ==================

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
                    "Không thể phát MessageCreated cho MessageId {MessageId}. Tin nhắn đã được lưu vào database.",
                    response.MessageId);
            }
        }

        private async Task PublishConversationUpdatedSafelyAsync(Guid negotiationId, Guid sellerId, Guid buyerId, MessageResponse lastMessage, NegotiationStatus status, decimal? price, int quantity)
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

        private negotiation CreateAgreedNegotiation(offer offer, post post, DateTime now)
        {
            var negotiation = _mapper.Map<negotiation>(offer);

            negotiation.NegotiationId = Guid.NewGuid();

            // Chấp nhận nguyên Offer nên đây là điều khoản cuối đã thống nhất.
            negotiation.FinalPrice = offer.OfferPrice;
            negotiation.FinalQuantity = offer.OfferQuantity;

            negotiation.NegotiationStatus = NegotiationStatus.Agreed;
            negotiation.LastMessageAt = now;
            negotiation.CreatedAt = now;

            AssignParticipants(negotiation, offer, post);

            return negotiation;
        }

        private static void AssignParticipants(negotiation negotiation, offer offer, post post)
        {
            // Bài Buy: chủ bài là Buyer, người gửi Offer là Seller.
            if (post.PostType == PostType.Buy)
            {
                negotiation.BuyerId = offer.ReceiverId;
                negotiation.SellerId = offer.SenderId;
                return;
            }

            // Bài Sell: chủ bài là Seller, người gửi Offer là Buyer.
            negotiation.SellerId = offer.ReceiverId;
            negotiation.BuyerId = offer.SenderId;
        }

        private async Task PublishOfferUpdatedSafelyAsync(offer offer)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = _mapper.Map<OfferResponse>(offer);

                await _realtimePublisher.PublishOfferUpdatedAsync(
                    new[] { offer.SenderId, offer.ReceiverId },
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Không thể phát OfferUpdated cho OfferId {OfferId}.", offer.OfferId);
            }
        }

        private async Task PublishOfferCreatedSafelyAsync(offer offer)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _realtimePublisher.PublishOfferCreatedAsync(
                    offer.ReceiverId,
                    _mapper.Map<OfferResponse>(offer),
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát OfferCreated cho OfferId {OfferId}.",
                    offer.OfferId);
            }
        }

        private Task<notification> AddOfferNotificationPendingAsync(offer offer, Guid actorId, string title, string message, CancellationToken cancellationToken)
        {
            Guid recipientId;

            if (actorId == offer.SenderId)
                recipientId = offer.ReceiverId;
            else if (actorId == offer.ReceiverId)
                recipientId = offer.SenderId;
            else
                throw new InvalidOperationException("Người thực hiện không thuộc Offer");

            return _notificationService.AddPendingAsync(
                new CreateNotificationCommand(
                    recipientId,
                    title,
                    message,
                    NotificationTargetType.Offer,
                    offer.OfferId),
                cancellationToken);
        }
    }
}
