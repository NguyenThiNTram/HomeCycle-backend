using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Negotiates;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using MathNet.Numerics.Distributions;
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
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<NegotiationService> _logger;
        private readonly IValidator<SendNegotiationCounterRequest> _counterValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRealtimePublisher _realtimePublisher;

        private const decimal MinPriceFactor = 0.2m;
        private const decimal MaxPriceFactor = 3m;

        public NegotiationService(
            INegotiationRepository negotiationRepository,
            IOfferRepository offerRepository,
            IMessageRepository messageRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ILogger<NegotiationService> logger,
            IValidator<SendNegotiationCounterRequest> counterValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IChatRealtimePublisher realtimePublisher)
        {
            _negotiationRepository = negotiationRepository;
            _offerRepository = offerRepository;
            _messageRepository = messageRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _logger = logger;
            _counterValidator = counterValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _realtimePublisher = realtimePublisher;
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

            return Result<NegotiationDetailResponse>.Success(ToDetailResponse(negotiation, messages.Items));
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

            return Result<NegotiationDetailResponse>.Success(ToDetailResponse(negotiation, messages.Items));
        }

        public async Task<Result<PagedResult<NegotiationListItemResponse>>> GetMyNegotiationsAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _negotiationRepository.GetByParticipantAsync(userId, request, cancellationToken);

            var items = new List<NegotiationListItemResponse>();
            foreach (var n in paged.Items)
            {
                var item = ToListItemResponse(n, userId);
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

                //Chỉ cho gửi counter khi Open
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.InvalidStatusForCounter);
                }

                // Khóa dòng proposal Pending: chống 2 counter cùng lúc cùng supersede và tạo ra 2 proposal Pending trong cùng negotiation
                var pendingMessage = await _messageRepository.GetPendingProposalForUpdateAsync(negotiationId, cancellationToken);

                if (pendingMessage is not null && pendingMessage.SenderId == userId)
                {
                    pendingMessage.OfferStatus = MessageOfferStatus.Cancelled;

                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.Forbidden);
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

                if (post.Status != PostStatus.Active)
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

                var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice);
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

                systemMessage = CreateSystemMessage(negotiationId, userId, actorName, NegotiationSystemAction.Counter, now.AddTicks(10));

                //if (pendingMessage is not null)
                //{
                //    pendingMessage.OfferStatus = MessageOfferStatus.Superseded;
                //    pendingMessage.UpdatedAt = now;
                //    await _messageRepository.TryUpdateProposalStatusAsync(
                //         pendingMessage.MessageId,
                //         MessageOfferStatus.Pending,
                //         MessageOfferStatus.Superseded,
                //         now,
                //         cancellationToken);
                //}

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

                await _offerRepository.UpdateAsync(offer, cancellationToken);

                await _messageRepository.AddAsync(counterMessage, cancellationToken);
                await _messageRepository.AddAsync(systemMessage, cancellationToken);

                //negotiation.LastMessageAt = now;
                negotiation.LastMessageAt = systemMessage.CreatedAt;

                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

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

            if (supersededProposal is not null)
            {
                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    _mapper.Map<MessageResponse>(supersededProposal));
            }

            await PublishMessageCreatedSafelyAsync(negotiationId, counterResponse);
            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);

            await PublishOfferUpdatedSafelyAsync(committedOffer);

            // Giữ counter card làm dữ liệu preview, không dùng system message
            await PublishConversationUpdatedSafelyAsync(
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                counterResponse,
                committedNegotiation.NegotiationStatus
                    ?? NegotiationStatus.Open,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(
                ToActionResponse(committedNegotiation, committedOffer, counterMessage, systemMessage));
        }

        // Buyer hoặc Seller có thể chấp nhận proposal Pending - không được chấp nhận proposal do chính mình gửi
        public async Task<Result<NegotiationActionResponse>> AcceptProposalAsync(
            Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default)
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

                if (post.Status != PostStatus.Active)
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

                systemMessage = CreateSystemMessage(negotiationId, userId, actorName, NegotiationSystemAction.Accept, now);

                //proposal.OfferStatus = MessageOfferStatus.Accepted;
                //await _messageRepository.TryUpdateProposalStatusAsync(
                //    proposal.MessageId,
                //    MessageOfferStatus.Pending,
                //    MessageOfferStatus.Accepted,
                //    now,
                //    cancellationToken);

                negotiation.NegotiationStatus = NegotiationStatus.Agreed;
                negotiation.FinalPrice = proposal.OfferPrice;
                negotiation.FinalQuantity = proposal.OfferQuantity;
                negotiation.LastMessageAt = DateTime.UtcNow;

                await _messageRepository.AddAsync(systemMessage, cancellationToken);
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                //offer.OfferPrice = proposal.OfferPrice;
                //offer.OfferQuantity = proposal.OfferQuantity;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
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

            await PublishOfferUpdatedSafelyAsync(committedOffer);
            await PublishMessageUpdatedSafelyAsync(negotiationId, proposalResponse);
            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);

            await PublishConversationUpdatedSafelyAsync(
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                proposalResponse,
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
        // Negotiation vẫn Open; Offer không bị Rejected; hai bên vẫn có thể counter tiếp.
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
                // Khóa dòng Negotiation: serialize reject/counter/accept đồng thời trong cùng negotiation.
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

                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationActionResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // Khóa dòng proposal được chỉ định để không reject đè lên proposal đã bị counter/supersede.
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

                proposal.OfferStatus = MessageOfferStatus.Rejected;
                proposal.UpdatedAt = now;

                systemMessage = CreateSystemMessage(
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

            await PublishMessageUpdatedSafelyAsync(negotiationId, proposalResponse);
            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);

            await PublishConversationUpdatedSafelyAsync(
                negotiationId,
                committedNegotiation.SellerId,
                committedNegotiation.BuyerId,
                proposalResponse,
                committedNegotiation.NegotiationStatus
                    ?? NegotiationStatus.Open,
                committedOffer.OfferPrice,
                committedOffer.OfferQuantity,
                committedOffer.Version);

            return Result<NegotiationActionResponse>.Success(
                ToActionResponse(
                    committedNegotiation,
                    committedOffer,
                    rejectedProposal,
                    systemMessage));
        }

        // Một trong hai bên chủ động hủy phiên thương lượng mà chưa đạt thỏa thuận.
        public async Task<Result<NegotiationActionResponse>> CancelAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var actorName = await GetActorNameAsync(userId, cancellationToken);

            negotiation committedNegotiation = null!;
            offer committedOffer = null!;
            message systemMessage = null!;
            message? cancelledProposal = null;

            var cancelledAt = DateTime.MinValue;

            //MessageResponse? cancelledProposalResponse = null;
            //NegotiationResponse? response = null;

            //negotiation? cancelledNegotiation = null;
            //offer? cancelledOffer = null;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
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

                // Chỉ được hủy khi hai bên vẫn đang thương lượng.
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
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
                    negotiationId,
                    userId,
                    actorName,
                    NegotiationSystemAction.Cancel,
                    cancelledAt);

                negotiation.NegotiationStatus = NegotiationStatus.Cancelled;
                negotiation.LastMessageAt = cancelledAt;
                offer.OfferStatus = OfferStatus.Cancelled;

                await _messageRepository.AddAsync(systemMessage, cancellationToken);

                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _offerRepository.UpdateAsync(offer, cancellationToken);
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

            if (cancelledProposal is not null)
            {
                await PublishMessageUpdatedSafelyAsync(negotiationId,
                    _mapper.Map<MessageResponse>(cancelledProposal));
            }

            var systemResponse = _mapper.Map<MessageResponse>(systemMessage);

            await PublishOfferUpdatedSafelyAsync(committedOffer);
            await PublishMessageCreatedSafelyAsync(negotiationId, systemResponse);

            await PublishConversationCancelledSafelyAsync(
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
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(
                    negotiationId,
                    cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Fail(NegotiationErrors.NotFound);
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

        // ================== PRIVATE HELPERS ==================

        private NegotiationDetailResponse ToDetailResponse(negotiation negotiation, IReadOnlyList<message> messages)
        {
            return new NegotiationDetailResponse
            {
                NegotiationId = negotiation.NegotiationId,
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

        private static NegotiationProposalResponse ToProposalResponse(message message, NegotiationStatus? negotiationStatus)
        {
            return new NegotiationProposalResponse
            {
                MessageId = message.MessageId,
                NegotiationId = message.NegotiationId,
                SenderId = message.SenderId,
                OfferPrice = message.OfferPrice,
                OfferQuantity = message.OfferQuantity,
                OfferStatus = message.OfferStatus ?? MessageOfferStatus.Pending,
                NegotiationStatus = negotiationStatus ?? NegotiationStatus.Open,
                CreatedAt = message.CreatedAt
            };
        }

        private static bool IsParticipant(negotiation negotiation, Guid userId)
        {
            return negotiation.BuyerId == userId || negotiation.SellerId == userId;
        }

        private Error? ValidatePriceRange(decimal? basePrice, decimal offerPrice)
        {
            if (!basePrice.HasValue)
                return OfferErrors.PriceOutOfRange(0, 0);

            var minPrice = basePrice.Value * MinPriceFactor;
            var maxPrice = basePrice.Value * MaxPriceFactor;

            return offerPrice < minPrice || offerPrice > maxPrice
                ? OfferErrors.PriceOutOfRange(minPrice, maxPrice)
                : null;
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

        private async Task PublishConversationUpdatedSafelyAsync(Guid negotiationId, Guid sellerId, Guid buyerId, MessageResponse lastMessage, NegotiationStatus status, decimal? price, int quantity, int? version)
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
                        CurrentOfferVersion = version,
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

        private async Task PublishConversationCancelledSafelyAsync( Guid negotiationId, Guid sellerId, Guid buyerId, Guid cancelledBy, DateTime cancelledAt, decimal? price, int quantity, int? version)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var unread = await _messageRepository.GetUnreadCountsByNegotiationAsync(negotiationId, buyerId, sellerId, timeout.Token);

                await _realtimePublisher.PublishConversationUpdatedAsync(
                    new[] { sellerId, buyerId },
                    new ConversationUpdatedResponse
                    {
                        NegotiationId = negotiationId,
                        LastSenderId = cancelledBy,
                        LastMessagePreview = "Phiên thương lượng đã bị hủy.",
                        LastMessageType = null,
                        LastMessageAt = cancelledAt,
                        CurrentOfferPrice = price,
                        CurrentOfferQuantity = quantity,
                        CurrentOfferVersion = version,
                        NegotiationStatus = NegotiationStatus.Cancelled,
                        UnreadCountByUser = unread
                    }, timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,  "Không thể phát ConversationUpdated khi hủy NegotiationId {NegotiationId}.", negotiationId);
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
    }
}
