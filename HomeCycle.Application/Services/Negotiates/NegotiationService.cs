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
        public async Task<Result<NegotiationProposalResponse>> CounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _counterValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<NegotiationProposalResponse>.Fail(ToValidationError(validation));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa Negotiation: serialize 2 counter đồng thời trong cùng một negotiation
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(negotiationId, cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.Forbidden);
                }

                //Chỉ cho gửi counter khi Open
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.InvalidStatusForCounter);
                }

                // Khóa dòng proposal Pending: chống 2 counter cùng lúc cùng supersede và tạo ra 2 proposal Pending trong cùng negotiation
                var pendingMessage =
                    await _messageRepository.GetPendingProposalForUpdateAsync(
                        negotiationId,
                        cancellationToken);

                if (pendingMessage is not null && pendingMessage.SenderId == userId)
                {
                    pendingMessage.OfferStatus = MessageOfferStatus.Cancelled;

                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var post = await _postRepository.GetByIdAsync(
                    negotiation.PostId,
                    cancellationToken);

                if (post is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(OfferErrors.PostNotFound);
                }

                if (post.Status != PostStatus.Active)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(OfferErrors.PostNotActive);
                }

                if (request.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            request.OfferQuantity,
                            post.RemainingQuantity));
                }

                var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice);
                if (priceError is not null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(priceError);
                }

                var offer = await _offerRepository.GetByIdAsync(
                    negotiation.OfferId,
                    cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(OfferErrors.NotFound);
                }

                var now = DateTime.UtcNow;
                var counterMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiationId,
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

                if (pendingMessage is not null)
                {
                    pendingMessage.OfferStatus = MessageOfferStatus.Superseded;
                    pendingMessage.UpdatedAt = now;
                    await _messageRepository.TryUpdateProposalStatusAsync(
                         pendingMessage.MessageId,
                         MessageOfferStatus.Pending,
                         MessageOfferStatus.Superseded,
                         now,
                         cancellationToken);
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

                negotiation.LastMessageAt = now;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                if (pendingMessage is not null)
                {
                    await PublishMessageUpdatedSafelyAsync(
                        negotiationId,
                        _mapper.Map<MessageResponse>(pendingMessage));
                }
                // Realtime: đẩy counter mới cho cả 2 bên trong negotiation.
                await PublishMessageCreatedSafelyAsync(
                    negotiationId,
                    _mapper.Map<MessageResponse>(counterMessage));

                await PublishOfferUpdatedSafelyAsync(offer);

                // Realtime: cập nhật thẻ chat ngoài list cho cả 2 bên.
                await PublishConversationUpdatedSafelyAsync(
                    negotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(counterMessage),
                    negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                    counterMessage.OfferPrice,
                    counterMessage.OfferQuantity,
                    offer.Version);

                return Result<NegotiationProposalResponse>.Success(
                    ToProposalResponse(counterMessage, negotiation.NegotiationStatus));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // Bất kỳ participant nào (Buyer hoặc Seller) cũng có thể chấp nhận proposal Pending
        // của đối phương để chốt thương lượng. Không được chấp nhận proposal do chính mình gửi.
        public async Task<Result<NegotiationResponse>> AcceptProposalAsync(
            Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default)
        {
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
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotFound);
                }

                // Người gọi phải là participant (Buyer hoặc Seller) của phiên thương lượng.
                if (!NegotiationAccess.IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.Forbidden);
                }

                // Re-check trạng thái NGAY TRONG khóa.
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // Khóa dòng proposal được chỉ định: chống accept/đồng thời trên cùng proposal.
                var proposal = await _messageRepository.GetByIdForUpdateAsync(
                    proposalMessageId,
                    cancellationToken);

                if (proposal is null || proposal.NegotiationId != negotiationId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                if (proposal.OfferStatus != MessageOfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotPending);
                }

                // Không được tự Accept proposal do chính mình vừa gửi
                if (proposal.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var isProposal =
                    proposal.MessageType == MessageType.Offer ||
                    proposal.MessageType == MessageType.CounterOffer;

                if (!isProposal)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return Result<NegotiationResponse>.Fail(
                        NegotiationErrors.ProposalNotFound);
                }

                // Khóa dòng Post TRONG transaction: chống lost update khi 2 Negotiation khác nhau
                // cùng trừ RemainingQuantity của cùng một bài đăng.
                var post = await _postRepository.GetByIdForUpdateAsync(
                    negotiation.PostId,
                    cancellationToken);

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

                // Re-check tồn kho NGAY SAU KHI post được khóa (giá trị đã được refresh)
                if (proposal.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            proposal.OfferQuantity,
                            post.RemainingQuantity));
                }

                var offer = await _offerRepository.GetByIdAsync(
                    negotiation.OfferId,
                    cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotFound);
                }

                var termsChanged = offer.OfferPrice != proposal.OfferPrice || offer.OfferQuantity != proposal.OfferQuantity;
                if (termsChanged)
                {
                    offer.OfferPrice = proposal.OfferPrice;
                    offer.OfferQuantity = proposal.OfferQuantity;
                    //offer.Version++;
                    offer.Version = (offer.Version ?? 1) + 1;

                    await PublishOfferUpdatedSafelyAsync(offer);
                }

                var now = DateTime.UtcNow;

                proposal.OfferStatus = MessageOfferStatus.Accepted;
                await _messageRepository.TryUpdateProposalStatusAsync(
                    proposal.MessageId,
                    MessageOfferStatus.Pending,
                    MessageOfferStatus.Accepted,
                    now,
                    cancellationToken);

                negotiation.NegotiationStatus = NegotiationStatus.Agreed;
                negotiation.FinalPrice = proposal.OfferPrice;
                negotiation.FinalQuantity = proposal.OfferQuantity;
                negotiation.LastMessageAt = DateTime.UtcNow;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                //offer.OfferPrice = proposal.OfferPrice;
                //offer.OfferQuantity = proposal.OfferQuantity;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Realtime: thẻ proposal được chấp nhận → cập nhật trạng thái Accepted.
                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    _mapper.Map<MessageResponse>(proposal));

                // Realtime: cập nhật thẻ chat ngoài list khi negotiation sang trạng thái Agreed.
                await PublishConversationUpdatedSafelyAsync(
                    negotiationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    _mapper.Map<MessageResponse>(proposal),
                    NegotiationStatus.Agreed,
                    proposal.OfferPrice,
                    proposal.OfferQuantity,
                    offer.Version);

                return Result<NegotiationResponse>.Success(
                    _mapper.Map<NegotiationResponse>(negotiation));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // Chỉ từ chối proposal Pending của đối phương.
        // Negotiation vẫn Open; Offer không bị Rejected; hai bên vẫn có thể counter tiếp.
        public async Task<Result<NegotiationProposalResponse>> RejectProposalAsync(Guid userId, Guid negotiationId, Guid proposalMessageId, CancellationToken cancellationToken = default)
        {
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
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // Khóa dòng proposal được chỉ định để không reject đè lên proposal đã bị counter/supersede.
                var proposal = await _messageRepository.GetByIdForUpdateAsync(
                    proposalMessageId,
                    cancellationToken);

                if (proposal is null || proposal.NegotiationId != negotiationId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.ProposalNotFound);
                }

                if (proposal.OfferStatus != MessageOfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(OfferErrors.NotPending);
                }

                if (proposal.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationProposalResponse>.Fail(NegotiationErrors.Forbidden);
                }

                var isProposal =
                    proposal.MessageType == MessageType.Offer ||
                    proposal.MessageType == MessageType.CounterOffer;

                if (!isProposal)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return Result<NegotiationProposalResponse>.Fail(
                        NegotiationErrors.ProposalNotFound);
                }

                var now = DateTime.UtcNow;

                proposal.OfferStatus = MessageOfferStatus.Rejected;
                await _messageRepository.TryUpdateProposalStatusAsync(
                    proposal.MessageId,
                    MessageOfferStatus.Pending,
                    MessageOfferStatus.Rejected,
                    now,
                    cancellationToken);

                // Không đổi NegotiationStatus và không đổi OfferStatus.
                negotiation.LastMessageAt = DateTime.UtcNow;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Realtime: thẻ proposal bị từ chối → cập nhật trạng thái Rejected.
                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    _mapper.Map<MessageResponse>(proposal));

                return Result<NegotiationProposalResponse>.Success(
                    ToProposalResponse(proposal, negotiation.NegotiationStatus));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // Một trong hai bên chủ động hủy phiên thương lượng mà chưa đạt thỏa thuận.
        public async Task<Result<NegotiationResponse>> CancelAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            MessageResponse? cancelledProposalResponse = null;
            NegotiationResponse? response = null;

            negotiation? cancelledNegotiation = null;
            offer? cancelledOffer = null;

            DateTime cancelledAt = default;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(negotiationId, cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!TradingAccess.IsNegotiationParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.Forbidden);
                }

                // Chỉ được hủy khi hai bên vẫn đang thương lượng.
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotOpen);
                }

                var pendingProposal = await _messageRepository.GetPendingProposalForUpdateAsync(negotiationId, cancellationToken);

                var offer = await _offerRepository.GetByIdForUpdateAsync(negotiation.OfferId, cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotFound);
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
                        return Result<NegotiationResponse>.Fail(OfferErrors.NotPending);
                    }

                    // Đồng bộ object map realtime
                    pendingProposal.OfferStatus = MessageOfferStatus.Cancelled;
                    pendingProposal.UpdatedAt = cancelledAt;

                    cancelledProposalResponse = _mapper.Map<MessageResponse>(pendingProposal);
                }

                negotiation.NegotiationStatus = NegotiationStatus.Cancelled;
                negotiation.LastMessageAt = cancelledAt;
                offer.OfferStatus = OfferStatus.Cancelled;

                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                //return Result<NegotiationResponse>.Success(
                //    _mapper.Map<NegotiationResponse>(negotiation));

                response = _mapper.Map<NegotiationResponse>(negotiation);

                cancelledNegotiation = negotiation;
                cancelledOffer = offer;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            if (cancelledProposalResponse is not null)
            {
                await PublishMessageUpdatedSafelyAsync(
                    negotiationId,
                    cancelledProposalResponse);
            }

            await PublishOfferUpdatedSafelyAsync(cancelledOffer!);

            await PublishConversationCancelledSafelyAsync(
                negotiationId,
                cancelledNegotiation!.SellerId,
                cancelledNegotiation.BuyerId,
                userId,
                cancelledAt,
                cancelledOffer!.OfferPrice,
                cancelledOffer.OfferQuantity,
                cancelledOffer.Version);

            return Result<NegotiationResponse>.Success(response!);
        }

        // Chỉ gọi nội bộ sau khi Agreement/Order hoàn tất — không phải endpoint công khai.
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
    }
}
