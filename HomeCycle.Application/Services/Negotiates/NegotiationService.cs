using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Audits;
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
    public class NegotiationService : INegotiationService
    {
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IOfferRepository _offerRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<NegotiationService> _logger;
        private readonly IValidator<SendNegotiationCounterRequest> _counterValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRealtimePublisher _realtimePublisher;
        private readonly IAuditService _auditService;
        private readonly HomeCycle.Application.Interfaces.Repositories.Agreements.IAgreementFormRepository _agreementRepository;
        private readonly HomeCycle.Application.Interfaces.Services.Notifications.INotificationService _notificationService;

        public NegotiationService(
            INegotiationRepository negotiationRepository,
            IOfferRepository offerRepository,
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ILogger<NegotiationService> logger,
            IValidator<SendNegotiationCounterRequest> counterValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IChatRealtimePublisher realtimePublisher,
            IAuditService auditService,
            HomeCycle.Application.Interfaces.Services.Notifications.INotificationService notificationService,
            HomeCycle.Application.Interfaces.Repositories.Agreements.IAgreementFormRepository agreementRepository)
        {
            _negotiationRepository = negotiationRepository;
            _offerRepository = offerRepository;
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _logger = logger;
            _counterValidator = counterValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _realtimePublisher = realtimePublisher;
            _auditService = auditService;
            _notificationService = notificationService;
            _agreementRepository = agreementRepository;
        }

        public async Task<int> ExpireDueAsync(int batchSize, CancellationToken cancellationToken = default, Guid? postId = null)
        {
            var ids = await _messageRepository.GetDueIdsAsync(DateTime.UtcNow, postId, cancellationToken);
            var processed = 0;
            foreach (var batch in ids.Chunk(Math.Max(1, batchSize)))
            foreach (var id in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    var snapshot = await _negotiationRepository.GetByIdAsync(id, cancellationToken);
                    if (snapshot != null)
                    {
                        await _postRepository.LockAsync(snapshot.PostId, snapshot.Offer?.BuyPostId, cancellationToken);
                        var entity = await _negotiationRepository.GetByIdForUpdateAsync(id, cancellationToken);
                        if (entity != null && await ExpireLockedNegotiationAsync(entity, cancellationToken)) processed++;
                    }
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Không thể xử lý hết hạn thương lượng {NegotiationId}", id);
                }
                finally
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                    _unitOfWork.ClearTrackedEntities();
                }
            }
            return processed;
        }

        private async Task<bool> ExpireLockedNegotiationAsync(negotiation entity, CancellationToken ct)
        {
            if (entity.NegotiationStatus != NegotiationStatus.Open) return false;
            var proposal = await _messageRepository.GetPendingProposalForUpdateAsync(entity.NegotiationId, ct);
            if (proposal == null || !TradingPostRules.IsExpired(TradingPostRules.ResponseDeadline(proposal))) return false;
            var now = DateTime.UtcNow;
            if (!await _messageRepository.TryUpdateProposalStatusAsync(proposal.MessageId, MessageOfferStatus.Pending, MessageOfferStatus.Expired, now, ct)) return false;
            proposal.OfferStatus = MessageOfferStatus.Expired;
            proposal.UpdatedAt = now;
            entity.NegotiationStatus = NegotiationStatus.Expired;
            await _negotiationRepository.UpdateAsync(entity, ct);
            var offer = await _offerRepository.GetByIdForUpdateAsync(entity.OfferId, ct);
            if (offer != null)
            {
                offer.OfferStatus = OfferStatus.Expired;
                offer.Version = (offer.Version ?? 1) + 1;
                await _offerRepository.UpdateAsync(offer, ct);
                _unitOfWork.RegisterAfterCommit(() => PublishOfferUpdatedSafelyAsync(offer));
            }
            foreach (var recipient in new[] { entity.BuyerId, entity.SellerId }.Distinct())
            {
                var notification = await _notificationService.AddPendingAsync(
                    new HomeCycle.Application.DTOs.Responses.Notifications.CreateNotificationCommand(recipient,
                        "Thương lượng đã hết hạn", "Đề nghị trả giá đã quá 15 phút phản hồi. Phiên thương lượng đã đóng; bạn có thể gửi yêu cầu mới.",
                        NotificationTargetType.Offer, entity.OfferId), ct);
                _unitOfWork.RegisterAfterCommit(() => _notificationService.PublishCreatedSafelyAsync(notification));
            }
            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.BusinessOperation, Action = AuditActions.NegotiationExpire,
                Outcome = AuditOutcome.Success, ActorType = AuditActorType.System,
                TargetType = AuditTargetTypes.Negotiation, TargetId = entity.NegotiationId
            }, ct);
            var response = _mapper.Map<MessageResponse>(proposal);
            _unitOfWork.RegisterAfterCommit(async () =>
            {
                await PublishMessageUpdatedSafelyAsync(entity.NegotiationId, response);
                if (entity.ConversationId.HasValue)
                {
                    await PublishConversationMessageUpdatedSafelyAsync(entity.ConversationId.Value, response);
                    await PublishConversationUpdatedSafelyAsync(entity.ConversationId.Value, entity.NegotiationId,
                        entity.SellerId, entity.BuyerId, response, NegotiationStatus.Expired,
                        offer?.OfferPrice, offer?.OfferQuantity ?? 0, offer?.Version);
                }
            });
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        // ================== QUERY ==================

        public async Task<Result<NegotiationDetailResponse>> GetByIdAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
            if (negotiation is null)
                return Result<NegotiationDetailResponse>.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(negotiation, userId))
                return Result<NegotiationDetailResponse>.Fail(NegotiationErrors.Forbidden);

            var messages = await _messageRepository.GetByNegotiationIdAsync(
                negotiationId,
                new PaginationRequest { PageNumber = 1, PageSize = 100 },
                cancellationToken);

            var response = ToDetailResponse(negotiation, messages.Items);
            response.ResponseDeadlineAt = TradingPostRules.ResponseDeadline(await _messageRepository.GetPendingProposalByNegotiationAsync(negotiation.NegotiationId, cancellationToken));
            response.PaymentDeadlineAt = TradingPostRules.PaymentDeadline(await _agreementRepository.GetByNegotiationIdAsync(negotiation.NegotiationId, cancellationToken));
            return Result<NegotiationDetailResponse>.Success(response);
        }

        public async Task<Result<NegotiationDetailResponse>> GetByOfferIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offerId, cancellationToken);
            if (negotiation is null)
                return Result<NegotiationDetailResponse>.Fail(NegotiationErrors.NotFound);

            if (!IsParticipant(negotiation, userId))
                return Result<NegotiationDetailResponse>.Fail(NegotiationErrors.Forbidden);

            var messages = await _messageRepository.GetByNegotiationIdAsync(
                negotiation.NegotiationId,
                new PaginationRequest { PageNumber = 1, PageSize = 100 },
                cancellationToken);

            var response = ToDetailResponse(negotiation, messages.Items);
            response.ResponseDeadlineAt = TradingPostRules.ResponseDeadline(await _messageRepository.GetPendingProposalByNegotiationAsync(negotiation.NegotiationId, cancellationToken));
            response.PaymentDeadlineAt = TradingPostRules.PaymentDeadline(await _agreementRepository.GetByNegotiationIdAsync(negotiation.NegotiationId, cancellationToken));
            return Result<NegotiationDetailResponse>.Success(response);
        }

        public async Task<Result<PagedResult<NegotiationListItemResponse>>> GetMyNegotiationsAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _negotiationRepository.GetByParticipantAsync(userId, request, cancellationToken);

            var items = new List<NegotiationListItemResponse>();
            foreach (var n in paged.Items)
            {
                var item = ToListItemResponse(n, userId);
                item.ResponseDeadlineAt = TradingPostRules.ResponseDeadline(await _messageRepository.GetPendingProposalByNegotiationAsync(n.NegotiationId, cancellationToken));
                item.PaymentDeadlineAt = TradingPostRules.PaymentDeadline(await _agreementRepository.GetByNegotiationIdAsync(n.NegotiationId, cancellationToken));
                item.UnreadCount =
                    await _messageRepository.CountUnreadByNegotiationForUserAsync(
                        n.NegotiationId,
                        userId,
                        cancellationToken);
                items.Add(item);
            }

            var response = new PagedResult<NegotiationListItemResponse>
            {
                Items = items,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<NegotiationListItemResponse>>.Success(response);
        }

        // ================== NEGOTIATION ACTIONS ==================

        // Một bên gửi proposal mới -> Không được tự counter đè lên proposal Pending do chính mình vừa gửi -> Proposal Pending của đối phương sẽ thành Superseded
        public async Task<Result<NegotiationActionResponse>> CounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _counterValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<NegotiationActionResponse>.Fail(ToValidationError(validation));

            var actorName = await GetActorNameAsync(userId, cancellationToken);

            negotiation committedNegotiation = null!;
            offer committedOffer = null!;
            message counterMessage = null!;
            message systemMessage = null!;
            message? supersededProposal = null;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa Negotiation: serialize 2 counter đồng thời trong cùng một negotiation
                var contextSnapshot = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepository.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(negotiationId, cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus == NegotiationStatus.Expired || await ExpireLockedNegotiationAsync(negotiation, cancellationToken))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Expired);
                }
                var conversationId = RequireConversationId(negotiation);

                //Chỉ cho gửi counter khi Open
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.InvalidStatusForCounter);
                }

                var pendingProposal = await _messageRepository.GetPendingProposalForUpdateAsync(negotiationId, cancellationToken);

                // Không được tự counter khi proposal Pending hiện tại do chính mình gửi
                if (pendingProposal is not null && pendingProposal.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var post = await _postRepository.GetByIdAsync(negotiation.PostId, cancellationToken);

                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status is PostStatus.Deleted or PostStatus.Suspended)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.PostNotActive);
                }

                if (request.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            request.OfferQuantity,
                            post.RemainingQuantity));
                }

                var priceError = await _postRepository.ValidateCapacityAsync(negotiation.Offer!, request.OfferQuantity, negotiationId, false, cancellationToken)
                    ?? new HomeCycle.Application.Services.Offers.OfferTermsPolicy().Validate(post, request.OfferPrice, request.OfferQuantity, negotiation.Offer?.BuyPostId != null);
                if (priceError is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(priceError);
                }

                var offer = await _offerRepository.GetByIdAsync(
                    negotiation.OfferId,
                    cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotFound);
                }

                var now = DateTime.UtcNow;
                counterMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiationId,
                    ConversationId = conversationId,
                    SenderId = userId,
                    ClientMessageId = null,
                    MessageType = MessageType.CounterOffer,
                    MessageContent = null,
                    OfferPrice = request.OfferPrice,
                    OfferQuantity = request.OfferQuantity,
                    OfferStatus = MessageOfferStatus.Pending,
                    MediaUrl = null,
                    BasePriceSnapshot = post.BasePrice,
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                systemMessage = CreateSystemMessage(
                    conversationId,
                    negotiationId,
                    userId,
                    actorName,
                    NegotiationSystemAction.Counter,
                    now.AddTicks(10));

                if (pendingProposal is not null)
                {
                    var updated =
                        await _messageRepository.TryUpdateProposalStatusAsync(
                            pendingProposal.MessageId,
                            MessageOfferStatus.Pending,
                            MessageOfferStatus.Superseded,
                            now,
                            cancellationToken);

                    if (!updated)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<NegotiationActionResponse>.Fail(
                            OfferErrors.NotPending);
                    }

                    pendingProposal.OfferStatus =
                        MessageOfferStatus.Superseded;

                    pendingProposal.UpdatedAt = now;
                    supersededProposal = pendingProposal;
                }

                var previousOfferPrice = offer.OfferPrice;
                var previousOfferQuantity = offer.OfferQuantity;
                var previousOfferVersion = offer.Version;
                // Đồng bộ Offer mới nhất
                //offer.OfferPrice = request.OfferPrice;
                //offer.OfferQuantity = request.OfferQuantity;
                var termsChanged = offer.OfferPrice != request.OfferPrice || offer.OfferQuantity != request.OfferQuantity;
                if (termsChanged)
                {
                    offer.OfferPrice = request.OfferPrice;
                    offer.OfferQuantity = request.OfferQuantity;
                    //offer.Version++;
                    offer.Version = (offer.Version ?? 1) + 1;
                }

                var negotiationCounterAuditDiff = new AuditDiffBuilder()
                    .Add("offerPrice", previousOfferPrice, offer.OfferPrice)
                    .Add("offerQuantity", previousOfferQuantity, offer.OfferQuantity)
                    .Add("version", previousOfferVersion, offer.Version);

                await _offerRepository.UpdateAsync(offer, cancellationToken);

                await _messageRepository.AddAsync(counterMessage, cancellationToken);
                await _messageRepository.AddAsync(systemMessage, cancellationToken);

                //negotiation.LastMessageAt = now;
                negotiation.LastMessageAt = systemMessage.CreatedAt;

                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversationId, systemMessage.CreatedAt, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.NegotiationCounter,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Negotiation,
                    TargetId = negotiation.NegotiationId,
                    OldValues = negotiationCounterAuditDiff.OldValues,
                    NewValues = negotiationCounterAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["proposalMessageId"] = counterMessage.MessageId
                    }
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                committedNegotiation = negotiation;
                committedOffer = offer;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var counterResponse = _mapper.Map<MessageResponse>(counterMessage);
            var systemResponse = _mapper.Map<MessageResponse>(systemMessage);
            var committedConversationId = RequireConversationId(committedNegotiation);

            if (supersededProposal is not null)
            {
                var supersededResponse = _mapper.Map<MessageResponse>(supersededProposal);

                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    supersededResponse);

                await PublishConversationMessageUpdatedSafelyAsync(
                    committedConversationId,
                    supersededResponse);
            }

            await PublishMessageCreatedSafelyAsync(negotiationId, counterResponse);
            await PublishConversationMessageCreatedSafelyAsync(committedConversationId, counterResponse);

            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);
            await PublishConversationMessageCreatedSafelyAsync(committedConversationId, systemResponse);

            await PublishOfferUpdatedSafelyAsync(committedOffer);

            await PublishConversationUpdatedSafelyAsync(
                committedConversationId,
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                systemResponse,
                committedNegotiation.NegotiationStatus
                    ?? NegotiationStatus.Open,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(
                ToActionResponse(committedNegotiation, committedOffer, counterMessage, systemMessage));
        }

        // Buyer hoặc Seller có thể chấp nhận proposal Pending - không được chấp nhận proposal do chính mình gửi
        public async Task<Result<NegotiationActionResponse>> AcceptProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default)
        {
            var actorName = await GetActorNameAsync(userId, cancellationToken);

            negotiation committedNegotiation = null!;
            offer committedOffer = null!;
            message acceptedProposal = null!;
            message systemMessage = null!;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa dòng Negotiation: chống accept 2 lần trên cùng negotiation (2 lần trừ tồn kho).
                var contextSnapshot = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepository.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(
                    negotiationId,
                    cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!NegotiationAccess.IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus == NegotiationStatus.Expired || await ExpireLockedNegotiationAsync(negotiation, cancellationToken))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Expired);
                }
                var conversationId = RequireConversationId(negotiation);

                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // chống accept/đồng thời trên cùng proposals
                var proposal = await _messageRepository.GetByIdForUpdateAsync(
                    proposalMessageId,
                    cancellationToken);

                if (proposal is null || proposal.NegotiationId != negotiationId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                if (proposal.OfferStatus != MessageOfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotPending);
                }

                // Không được tự Accept proposal do chính mình vừa gửi
                if (proposal.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var isProposal =
                    proposal.MessageType == MessageType.Offer ||
                    proposal.MessageType == MessageType.CounterOffer;

                if (!isProposal)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                // Khóa dòng Post TRONG transaction: chống lost update khi 2 Negotiation khác nhau
                // cùng trừ RemainingQuantity của cùng một bài đăng.
                var post = await _postRepository.GetByIdForUpdateAsync(
                    negotiation.PostId,
                    cancellationToken);

                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status is PostStatus.Deleted or PostStatus.Suspended)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.PostNotActive);
                }

                // Re-check tồn kho NGAY SAU KHI post được khóa (giá trị đã được refresh)
                if (proposal.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            proposal.OfferQuantity,
                            post.RemainingQuantity));
                }

                var offer = await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotFound);
                }

                var capacityError = await _postRepository.ValidateCapacityAsync(offer, proposal.OfferQuantity, negotiationId, false, cancellationToken)
                    ?? new HomeCycle.Application.Services.Offers.OfferTermsPolicy().Validate(post, proposal.OfferPrice!.Value, proposal.OfferQuantity, offer.BuyPostId.HasValue);
                if (capacityError != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(capacityError);
                }
                var now = DateTime.UtcNow;

                var proposalUpdated =
                    await _messageRepository.TryUpdateProposalStatusAsync(
                        proposal.MessageId,
                        MessageOfferStatus.Pending,
                        MessageOfferStatus.Accepted,
                        now,
                        cancellationToken);

                if (!proposalUpdated)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotPending);
                }

                proposal.OfferStatus = MessageOfferStatus.Accepted;
                proposal.UpdatedAt = now;

                var termsChanged = offer.OfferPrice != proposal.OfferPrice || offer.OfferQuantity != proposal.OfferQuantity;
                if (termsChanged)
                {
                    offer.OfferPrice = proposal.OfferPrice;
                    offer.OfferQuantity = proposal.OfferQuantity;
                    //offer.Version++;
                    offer.Version = (offer.Version ?? 1) + 1;
                }

                systemMessage = CreateSystemMessage(
                    conversationId,
                    negotiationId,
                    userId,
                    actorName,
                    NegotiationSystemAction.Accept,
                    now);

                //proposal.OfferStatus = MessageOfferStatus.Accepted;
                //await _messageRepository.TryUpdateProposalStatusAsync(
                //    proposal.MessageId,
                //    MessageOfferStatus.Pending,
                //    MessageOfferStatus.Accepted,
                //    now,
                //    cancellationToken);
                var previousNegotiationStatus = negotiation.NegotiationStatus;
                var previousFinalPrice = negotiation.FinalPrice;
                var previousFinalQuantity = negotiation.FinalQuantity;
                negotiation.NegotiationStatus = NegotiationStatus.Agreed;
                negotiation.FinalPrice = proposal.OfferPrice;
                negotiation.FinalQuantity = proposal.OfferQuantity;
                //negotiation.LastMessageAt = DateTime.UtcNow;
                negotiation.LastMessageAt = systemMessage.CreatedAt;
                var acceptNegotiationAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousNegotiationStatus?.ToString(), negotiation.NegotiationStatus?.ToString())
                    .Add("finalPrice", previousFinalPrice, negotiation.FinalPrice)
                    .Add("finalQuantity", previousFinalQuantity, negotiation.FinalQuantity);

                await _messageRepository.AddAsync(systemMessage, cancellationToken);
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversationId, systemMessage.CreatedAt, cancellationToken);

                //offer.OfferPrice = proposal.OfferPrice;
                //offer.OfferQuantity = proposal.OfferQuantity;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.NegotiationAccept,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Negotiation,
                    TargetId = negotiation.NegotiationId,
                    OldValues = acceptNegotiationAuditDiff.OldValues,
                    NewValues = acceptNegotiationAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["proposalMessageId"] = proposalMessageId
                    }
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                committedNegotiation = negotiation;
                committedOffer = offer;
                acceptedProposal = proposal;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var proposalResponse = _mapper.Map<MessageResponse>(acceptedProposal);
            var systemResponse = _mapper.Map<MessageResponse>(systemMessage);
            var committedConversationId = RequireConversationId(committedNegotiation);

            await PublishOfferUpdatedSafelyAsync(committedOffer);
            await PublishMessageUpdatedSafelyAsync(negotiationId, proposalResponse);
            await PublishConversationMessageUpdatedSafelyAsync(committedConversationId, proposalResponse);

            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);
            await PublishConversationMessageCreatedSafelyAsync(committedConversationId, systemResponse);

            await PublishConversationUpdatedSafelyAsync(
                committedConversationId,
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                systemResponse,
                NegotiationStatus.Agreed,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(
                ToActionResponse(
                    committedNegotiation,
                    committedOffer,
                    acceptedProposal,
                    systemMessage));
        }

        // Chỉ từ chối proposal Pending của đối phương.
        // Negotiation vẫn Open -> Offer không bị Rejected -> hai bên vẫn có thể counter tiếp
        public async Task<Result<NegotiationActionResponse>> RejectProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default)
        {
            var actorName = await GetActorNameAsync(userId, cancellationToken);

            negotiation committedNegotiation = null!;
            offer committedOffer = null!;
            message rejectedProposal = null!;
            message systemMessage = null!;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // serialize reject/counter/accept cùng lúc
                var contextSnapshot = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepository.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(
                    negotiationId,
                    cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus == NegotiationStatus.Expired || await ExpireLockedNegotiationAsync(negotiation, cancellationToken))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Expired);
                }
                var conversationId = RequireConversationId(negotiation);

                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // không reject đè lên proposal đã bị counter/supersede
                var proposal = await _messageRepository.GetByIdForUpdateAsync(
                    proposalMessageId,
                    cancellationToken);

                if (proposal is null || proposal.NegotiationId != negotiationId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                if (proposal.OfferStatus != MessageOfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotPending);
                }

                if (proposal.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var isProposal =
                    proposal.MessageType == MessageType.Offer ||
                    proposal.MessageType == MessageType.CounterOffer;

                if (!isProposal)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                var offer = await _offerRepository.GetByIdAsync(negotiation.OfferId, cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotFound);
                }

                var now = DateTime.UtcNow;

                var proposalUpdated =
                    await _messageRepository.TryUpdateProposalStatusAsync(
                        proposal.MessageId,
                        MessageOfferStatus.Pending,
                        MessageOfferStatus.Rejected,
                        now,
                        cancellationToken);

                if (!proposalUpdated)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(
                        OfferErrors.NotPending);
                }
                var previousProposalStatus = proposal.OfferStatus;
                proposal.OfferStatus = MessageOfferStatus.Rejected;
                proposal.UpdatedAt = now;
                var rejectProposalAuditDiff = new AuditDiffBuilder()
                    .Add("proposalStatus", previousProposalStatus?.ToString(), proposal.OfferStatus?.ToString());

                systemMessage = CreateSystemMessage(
                    conversationId,
                    negotiationId,
                    userId,
                    actorName,
                    NegotiationSystemAction.Reject,
                    now);

                negotiation.LastMessageAt = systemMessage.CreatedAt;

                // Không đổi NegotiationStatus và không đổi OfferStatus.
                //negotiation.LastMessageAt = DateTime.UtcNow;
                await _messageRepository.AddAsync(systemMessage, cancellationToken);
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversationId, systemMessage.CreatedAt, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.NegotiationReject,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Negotiation,
                    TargetId = negotiation.NegotiationId,
                    OldValues = rejectProposalAuditDiff.OldValues,
                    NewValues = rejectProposalAuditDiff.NewValues,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["proposalMessageId"] = proposalMessageId
                    }
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                committedNegotiation = negotiation;
                committedOffer = offer;
                rejectedProposal = proposal;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var proposalResponse = _mapper.Map<MessageResponse>(rejectedProposal);
            var systemResponse = _mapper.Map<MessageResponse>(systemMessage);
            var committedConversationId = RequireConversationId(committedNegotiation);

            await PublishMessageUpdatedSafelyAsync(negotiationId, proposalResponse);
            await PublishConversationMessageUpdatedSafelyAsync(committedConversationId, proposalResponse);

            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);
            await PublishConversationMessageCreatedSafelyAsync(committedConversationId, systemResponse);

            await PublishConversationUpdatedSafelyAsync(
                committedConversationId,
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                systemResponse,
                committedNegotiation.NegotiationStatus
                    ?? NegotiationStatus.Open,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(ToActionResponse(committedNegotiation, committedOffer, rejectedProposal, systemMessage));
        }

        // Một trong hai bên chủ động hủy phiên thương lượng mà chưa đạt thỏa thuận
        public async Task<Result<NegotiationActionResponse>> CancelAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var actorName = await GetActorNameAsync(userId, cancellationToken);

            negotiation committedNegotiation = null!;
            offer committedOffer = null!;
            message systemMessage = null!;
            message? cancelledProposal = null;

            var cancelledAt = DateTime.MinValue;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var contextSnapshot = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepository.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(negotiationId, cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!TradingAccess.IsNegotiationParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus == NegotiationStatus.Expired || await ExpireLockedNegotiationAsync(negotiation, cancellationToken))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Expired);
                }
                var conversationId = RequireConversationId(negotiation);

                // Chỉ được hủy khi hai bên vẫn đang thương lượng
                if (negotiation.NegotiationStatus is not (NegotiationStatus.Open or NegotiationStatus.Agreed) ||
                    await _negotiationRepository.HasAgreementAsync(negotiationId, cancellationToken))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotOpen);
                }

                var pendingProposal = await _messageRepository.GetPendingProposalForUpdateAsync(negotiationId, cancellationToken);

                var offer = await _offerRepository.GetByIdForUpdateAsync(negotiation.OfferId, cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(OfferErrors.NotFound);
                }

                //var now = DateTime.UtcNow;
                cancelledAt = DateTime.UtcNow;

                if (pendingProposal is not null)
                {
                    var proposalUpdated =
                        await _messageRepository.TryUpdateProposalStatusAsync(
                            pendingProposal.MessageId,
                            MessageOfferStatus.Pending,
                            MessageOfferStatus.Cancelled,
                            cancelledAt,
                            cancellationToken);

                    if (!proposalUpdated)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<NegotiationActionResponse>.Fail(OfferErrors.NotPending);
                    }

                    // Đồng bộ object map realtime
                    pendingProposal.OfferStatus = MessageOfferStatus.Cancelled;
                    pendingProposal.UpdatedAt = cancelledAt;
                    cancelledProposal = pendingProposal;

                    //cancelledProposalResponse = _mapper.Map<MessageResponse>(pendingProposal);
                }

                systemMessage = CreateSystemMessage(
                    conversationId,
                    negotiationId,
                    userId,
                    actorName,
                    NegotiationSystemAction.Cancel,
                    cancelledAt);

                var previousNegotiationStatus = negotiation.NegotiationStatus;
                negotiation.NegotiationStatus = NegotiationStatus.Cancelled;
                negotiation.LastMessageAt = cancelledAt;
                offer.OfferStatus = OfferStatus.Cancelled;
                var cancelNegotiationAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousNegotiationStatus?.ToString(), negotiation.NegotiationStatus?.ToString());

                await _messageRepository.AddAsync(systemMessage, cancellationToken);
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _conversationRepository.UpdateLastActivityAsync(conversationId, systemMessage.CreatedAt, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.NegotiationCancel,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = userId,
                    TargetType = AuditTargetTypes.Negotiation,
                    TargetId = negotiation.NegotiationId,
                    OldValues = cancelNegotiationAuditDiff.OldValues,
                    NewValues = cancelNegotiationAuditDiff.NewValues
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                committedNegotiation = negotiation;
                committedOffer = offer;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var committedConversationId = RequireConversationId(committedNegotiation);

            if (cancelledProposal is not null)
            {
                var cancelledProposalResponse = _mapper.Map<MessageResponse>(cancelledProposal);

                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    cancelledProposalResponse);

                await PublishConversationMessageUpdatedSafelyAsync(
                    committedConversationId,
                    cancelledProposalResponse);
            }

            var systemResponse = _mapper.Map<MessageResponse>(systemMessage);

            await PublishOfferUpdatedSafelyAsync(committedOffer);
            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);
            await PublishConversationMessageCreatedSafelyAsync(committedConversationId, systemResponse);

            await PublishConversationCancelledSafelyAsync(
                committedConversationId,
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                userId,
                cancelledAt,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(
                ToActionResponse(
                    committedNegotiation,
                    committedOffer,
                    cancelledProposal,
                    systemMessage));
        }

        // Chỉ gọi nội bộ sau khi Agreement/Order hoàn tất
        public async Task<Result> CloseAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var contextSnapshot = await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepository.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(
                    negotiationId,
                    cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Fail(NegotiationErrors.NotFound);
                }

                if (negotiation.NegotiationStatus == NegotiationStatus.Expired || await ExpireLockedNegotiationAsync(negotiation, cancellationToken))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result.Fail(NegotiationErrors.Expired);
                }
                negotiation.NegotiationStatus = NegotiationStatus.Completed;
                negotiation.LastMessageAt = DateTime.UtcNow;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                var offer = await _offerRepository.GetByIdAsync(
                    negotiation.OfferId,
                    cancellationToken);

                if (offer is not null)
                {
                    offer.OfferStatus = OfferStatus.Completed;
                    await _offerRepository.UpdateAsync(offer, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result.Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<PagedResult<ModeratorNegotiationListItemDto>>> GetDisputedForModeratorAsync(
            ModeratorNegotiationSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _negotiationRepository.GetDisputedForModeratorAsync(request, cancellationToken);

            return Result<PagedResult<ModeratorNegotiationListItemDto>>.Success(result);
        }

        public async Task<Result<ModeratorNegotiationDetailDto>> GetDisputedDetailForModeratorAsync(
            Guid negotiationId,
            CancellationToken cancellationToken = default)
        {
            var detail = await _negotiationRepository.GetDisputedDetailForModeratorAsync(
                negotiationId,
                cancellationToken);

            if (detail is null)
                return Result<ModeratorNegotiationDetailDto>.Fail(NegotiationErrors.NotFound);

            return Result<ModeratorNegotiationDetailDto>.Success(detail);
        }

        public async Task<Result<PagedResult<ModeratorNegotiationMessageDto>>> GetDisputedMessagesForModeratorAsync(
            Guid negotiationId,
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetDisputedDetailForModeratorAsync(
                negotiationId,
                cancellationToken);

            if (negotiation is null)
                return Result<PagedResult<ModeratorNegotiationMessageDto>>
                    .Fail(NegotiationErrors.NotFound);

            var messages = await _messageRepository.GetByNegotiationIdAsync(
                negotiationId,
                request,
                cancellationToken);

            var items = messages.Items.Select(message =>
            {
                string? senderUsername = null;

                if (message.SenderId == negotiation.BuyerId)
                    senderUsername = negotiation.BuyerUsername;
                else if (message.SenderId == negotiation.SellerId)
                    senderUsername = negotiation.SellerUsername;

                var isProposal = message.MessageType is MessageType.Offer or MessageType.CounterOffer;

                return new ModeratorNegotiationMessageDto
                {
                    MessageId = message.MessageId,
                    SenderId = message.SenderId,
                    SenderUsername = senderUsername,
                    MessageType = message.MessageType,
                    MessageContent = message.MessageContent,

                    OfferPrice = isProposal ? message.OfferPrice : null,
                    OfferQuantity = isProposal ? message.OfferQuantity : null,
                    OfferStatus = isProposal ? message.OfferStatus : null,
                    BasePriceSnapshot = isProposal ? message.BasePriceSnapshot : null,

                    MediaUrl = message.MediaUrl,
                    CreatedAt = message.CreatedAt
                };
            }).ToList();

            return Result<PagedResult<ModeratorNegotiationMessageDto>>.Success(
                new PagedResult<ModeratorNegotiationMessageDto>
                {
                    Items = items,
                    PageNumber = messages.PageNumber,
                    PageSize = messages.PageSize,
                    TotalCount = messages.TotalCount
                });
        }

        // ================== PRIVATE HELPERS ==================

        private NegotiationDetailResponse ToDetailResponse(negotiation negotiation, IReadOnlyList<message> messages)
        {
            return new NegotiationDetailResponse
            {
                NegotiationId = negotiation.NegotiationId,
                ConversationId = negotiation.ConversationId,
                OfferId = negotiation.OfferId,
                PostId = negotiation.PostId,
                SellerId = negotiation.SellerId,
                BuyerId = negotiation.BuyerId,
                NegotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                FinalPrice = negotiation.FinalPrice,
                FinalQuantity = negotiation.FinalQuantity,
                CurrentOfferPrice = negotiation.Offer?.OfferPrice,
                CurrentOfferQuantity = negotiation.Offer?.OfferQuantity ?? 0,
                CurrentOfferVersion = negotiation.Offer?.Version,
                LastMessageAt = negotiation.LastMessageAt,
                CreatedAt = negotiation.CreatedAt,
                Messages = messages
                    .Select(x => _mapper.Map<MessageResponse>(x))
                    .ToList()
            };
        }

        private static NegotiationListItemResponse ToListItemResponse(negotiation negotiation, Guid userId)
        {
            var otherPartyId = negotiation.BuyerId == userId
                ? negotiation.SellerId
                : negotiation.BuyerId;

            var otherParty = negotiation.BuyerId == userId
                ? negotiation.Seller
                : negotiation.Buyer;

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
                CurrentOfferQuantity = negotiation.Offer?.OfferQuantity ?? 0,
                CurrentOfferVersion = negotiation.Offer?.Version ?? 0,
                NegotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                LastMessageAt = negotiation.LastMessageAt,
                CreatedAt = negotiation.CreatedAt
            };
        }

        //private static NegotiationProposalResponse ToProposalResponse(message message, NegotiationStatus? negotiationStatus)
        //{
        //    return new NegotiationProposalResponse
        //    {
        //        MessageId = message.MessageId,
        //        NegotiationId = message.NegotiationId,
        //        SenderId = message.SenderId,
        //        OfferPrice = message.OfferPrice,
        //        OfferQuantity = message.OfferQuantity,
        //        OfferStatus = message.OfferStatus ?? MessageOfferStatus.Pending,
        //        NegotiationStatus = negotiationStatus ?? NegotiationStatus.Open,
        //        CreatedAt = message.CreatedAt
        //    };
        //}

        private static bool IsParticipant(negotiation negotiation, Guid userId)
        {
            return negotiation.BuyerId == userId || negotiation.SellerId == userId;
        }

        private static Error ToValidationError(FluentValidation.Results.ValidationResult validation)
        {
            var errors = string.Join("\n", validation.Errors.Select(x => x.ErrorMessage));
            return ValidationErrors.InvalidRequest(errors);
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

        private async Task PublishMessageUpdatedSafelyAsync(Guid negotiationId, MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishMessageUpdatedAsync(
                    negotiationId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát MessageUpdated cho MessageId {MessageId}. Trạng thái đã được lưu vào database.",
                    response.MessageId);
            }
        }

        private async Task PublishConversationMessageUpdatedSafelyAsync(
            Guid conversationId,
            MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _realtimePublisher.PublishConversationMessageUpdatedAsync(
                    conversationId,
                    response,
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationMessageUpdated cho MessageId {MessageId}.",
                    response.MessageId);
            }
        }

        private async Task PublishConversationUpdatedSafelyAsync(Guid conversationId, Guid negotiationId, Guid sellerId, Guid buyerId, MessageResponse lastMessage, NegotiationStatus status, decimal? price, int quantity, int? version)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var unreadDetails = await _messageRepository.GetUnreadCountsDetailAsync(conversationId, sellerId, buyerId, timeout.Token);

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
                        ResponseDeadlineAt = status == NegotiationStatus.Open
                            ? TradingPostRules.ResponseDeadline(await _messageRepository.GetPendingProposalByNegotiationAsync(negotiationId, timeout.Token)) : null,
                        PaymentDeadlineAt = TradingPostRules.PaymentDeadline(await _agreementRepository.GetByNegotiationIdAsync(negotiationId, timeout.Token)),

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

        private async Task PublishConversationCancelledSafelyAsync(Guid conversationId, Guid negotiationId, Guid sellerId, Guid buyerId, Guid cancelledBy, DateTime cancelledAt, decimal? price, int quantity, int? version)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var unreadDetails = await _messageRepository.GetUnreadCountsDetailAsync(conversationId, sellerId, buyerId, timeout.Token);

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
                        LastSenderId = cancelledBy,
                        LastMessagePreview = "Phiên thương lượng đã bị hủy.",
                        LastMessageType = null,
                        LastMessageAt = cancelledAt,
                        CurrentOfferPrice = price,
                        CurrentOfferQuantity = quantity,
                        CurrentOfferVersion = version,
                        NegotiationStatus = NegotiationStatus.Cancelled,

                        ConversationUnreadByUser = conversationUnread,
                        NegotiationUnreadByUser = negotiationUnread
                    }, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể phát ConversationUpdated khi hủy ConversationId {ConversationId}.",
                    conversationId);
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
                MessageType.System => m.MessageContent ?? "[Hệ thống]",
                _ => "[Tin nhắn]"
            };
        }

        private enum NegotiationSystemAction
        {
            Counter,
            Accept,
            Reject,
            Cancel
        }

        private async Task<string> GetActorNameAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var actor = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

            return string.IsNullOrWhiteSpace(actor?.Username)
                ? "không xác định"
                : actor.Username.Trim();
        }

        private static message CreateSystemMessage(
            Guid conversationId,
            Guid negotiationId,
            Guid actorId,
            string actorName,
            NegotiationSystemAction action,
            DateTime createdAt)
        {
            var content = action switch
            {
                NegotiationSystemAction.Counter =>
                    $"Người dùng {actorName} đã đề xuất mức giá mới trong phiên thương lượng.",

                NegotiationSystemAction.Accept =>
                    $"Người dùng {actorName} đã chấp nhận mức giá trong phiên thương lượng.",

                NegotiationSystemAction.Reject =>
                    $"Người dùng {actorName} đã từ chối mức giá trong phiên thương lượng.",

                NegotiationSystemAction.Cancel =>
                    $"Người dùng {actorName} đã hủy phiên thương lượng.",

                _ => "Phiên thương lượng đã được cập nhật."
            };

            return new message
            {
                MessageId = Guid.NewGuid(),
                NegotiationId = negotiationId,
                ConversationId = conversationId,
                SenderId = actorId,
                ClientMessageId = null,

                MessageType = MessageType.System,
                MessageContent = content,

                OfferPrice = null,
                OfferQuantity = 0,
                OfferStatus = null,

                MediaUrl = null,
                BasePriceSnapshot = null,

                IsRead = false,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
        }

        private NegotiationActionResponse ToActionResponse(negotiation negotiation, offer offer, message? proposal, message systemMessage)
        {
            return new NegotiationActionResponse
            {
                NegotiationId = negotiation.NegotiationId,
                ConversationId = negotiation.ConversationId,
                OfferId = offer.OfferId,

                NegotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                OfferStatus = offer.OfferStatus ?? OfferStatus.Pending,

                CurrentOfferPrice = offer.OfferPrice,
                CurrentOfferQuantity = offer.OfferQuantity,
                CurrentOfferVersion = offer.Version,

                Proposal = proposal is null ? null
                    : new NegotiationActionProposalResponse
                    {
                        MessageId = proposal.MessageId,
                        SenderId = proposal.SenderId,
                        OfferPrice = proposal.OfferPrice,
                        OfferQuantity = proposal.OfferQuantity,
                        OfferStatus = proposal.OfferStatus ?? MessageOfferStatus.Pending,
                        ResponseDeadlineAt = TradingPostRules.ResponseDeadline(proposal),
                        CreatedAt = proposal.CreatedAt
                    },

                SystemMessage = new SystemMessageResponse
                {
                    MessageId = systemMessage.MessageId,
                    SenderId = systemMessage.SenderId,
                    MessageType = MessageType.System,
                    MessageContent = systemMessage.MessageContent ?? string.Empty,
                    CreatedAt = systemMessage.CreatedAt
                }
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
