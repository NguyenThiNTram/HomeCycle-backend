using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Services.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Offers
{
    public class OfferService : IOfferService
    {
        private readonly IOfferRepository _offerRepository;
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IPostRepository _postRepository;
        private readonly IValidator<CreateOfferRequest> _createValidator;
        private readonly IValidator<UpdateOfferRequest> _updateValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        // Công thức giá: MinPrice = BasePrice * 0.2 (giảm tối đa 80%), MaxPrice = BasePrice * 3
        private const decimal MinPriceFactor = 0.2m;
        private const decimal MaxPriceFactor = 3m;

        public OfferService(
            IOfferRepository offerRepository,
            INegotiationRepository negotiationRepository,
            IMessageRepository messageRepository,
            IPostRepository postRepository,
            IValidator<CreateOfferRequest> createValidator,
            IValidator<UpdateOfferRequest> updateValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _offerRepository = offerRepository;
            _negotiationRepository = negotiationRepository;
            _messageRepository = messageRepository;
            _postRepository = postRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<OfferResponse>> CreateOfferAsync(
            Guid userId, CreateOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<OfferResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post is null)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);

            if (post.Status != (int)PostStatus.Active)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);

            if (post.OwnerId == userId)
                return Result<OfferResponse>.Fail(OfferErrors.CannotOfferOwnPost);

            if (request.OfferQuantity > post.RemainingQuantity)
                return Result<OfferResponse>.Fail(
                    OfferErrors.QuantityExceedsRemaining(request.OfferQuantity, post.RemainingQuantity));

            if (!post.BasePrice.HasValue)
                return Result<OfferResponse>.Fail(OfferErrors.PriceOutOfRange(0, 0));

            var minPrice = post.BasePrice.Value * MinPriceFactor;
            var maxPrice = post.BasePrice.Value * MaxPriceFactor;

            if (request.OfferPrice < minPrice || request.OfferPrice > maxPrice)
                return Result<OfferResponse>.Fail(OfferErrors.PriceOutOfRange(minPrice, maxPrice));

            if (await _offerRepository.ExistsPendingByPostAndSenderAsync(request.PostId, userId, cancellationToken))
                return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending);

            var now = DateTime.UtcNow;

            var offer = new offer
            {
                OfferId = Guid.NewGuid(),
                PostId = request.PostId,
                SenderId = userId,
                ReceiverId = post.OwnerId,
                OfferPrice = request.OfferPrice,
                OfferQuantity = request.OfferQuantity,
                OfferStatus = (int)OfferStatus.Pending,
                CreatedAt = now
            };

            var negotiation = new negotiation
            {
                NegotiationId = Guid.NewGuid(),
                PostId = request.PostId,
                OfferId = offer.OfferId,
                SellerId = post.OwnerId,
                BuyerId = userId,
                FinalPrice = null,
                FinalQuantity = null,
                LastMessageAt = now,
                CreatedAt = now,
                NegotiationStatus = (int)NegotiationStatus.Open
            };

            var message = new message
            {
                MessageId = Guid.NewGuid(),
                NegotiationId = negotiation.NegotiationId,
                SenderId = userId,
                MessageContent = "Đề nghị thương lượng ban đầu.",
                MessageType = (int)MessageType.CounterOffer,
                OfferPrice = request.OfferPrice,
                OfferQuantity = request.OfferQuantity,
                OfferStatus = (int)OfferStatus.Pending,
                BasePriceSnapshot = post.BasePrice.Value,
                IsRead = false,
                CreatedAt = now
            };

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _offerRepository.AddAsync(offer, cancellationToken);
                await _negotiationRepository.AddAsync(negotiation, cancellationToken);
                await _messageRepository.AddAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<OfferResponse>> GetByIdAsync(
            Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId && offer.ReceiverId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<PagedResult<OfferResponse>>> GetSentAsync(
            Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetSentAsync(userId, request, cancellationToken);

            var response = new PagedResult<OfferResponse>
            {
                Items = paged.Items.Select(x => _mapper.Map<OfferResponse>(x)).ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<OfferResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<OfferResponse>>> GetReceivedAsync(
            Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetReceivedAsync(userId, request, cancellationToken);

            var response = new PagedResult<OfferResponse>
            {
                Items = paged.Items.Select(x => _mapper.Map<OfferResponse>(x)).ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<OfferResponse>>.Success(response);
        }

        public async Task<Result<OfferResponse>> UpdateAsync(
            Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<OfferResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != (int)OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
            if (post is null)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);

            if (post.Status != (int)PostStatus.Active)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);

            if (request.OfferQuantity > post.RemainingQuantity)
                return Result<OfferResponse>.Fail(
                    OfferErrors.QuantityExceedsRemaining(request.OfferQuantity, post.RemainingQuantity));

            if (!post.BasePrice.HasValue)
                return Result<OfferResponse>.Fail(OfferErrors.PriceOutOfRange(0, 0));

            var minPrice = post.BasePrice.Value * MinPriceFactor;
            var maxPrice = post.BasePrice.Value * MaxPriceFactor;

            if (request.OfferPrice < minPrice || request.OfferPrice > maxPrice)
                return Result<OfferResponse>.Fail(OfferErrors.PriceOutOfRange(minPrice, maxPrice));

            offer.OfferPrice = request.OfferPrice;
            offer.OfferQuantity = request.OfferQuantity;

            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offer.OfferId, cancellationToken);
            if (negotiation is not null)
            {
                var counterMessage = await _messageRepository.GetPendingCounterOfferByNegotiationAsync(negotiation.NegotiationId, cancellationToken);
                if (counterMessage is not null)
                {
                    counterMessage.OfferPrice = request.OfferPrice;
                    counterMessage.OfferQuantity = request.OfferQuantity;
                    await _messageRepository.UpdateAsync(counterMessage, cancellationToken);
                }
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<OfferResponse>> CancelAsync(
            Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != (int)OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            offer.OfferStatus = (int)OfferStatus.Cancelled;

            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offer.OfferId, cancellationToken);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                if (negotiation is not null)
                {
                    negotiation.NegotiationStatus = (int)NegotiationStatus.Closed;
                    await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                    var counterMessage = await _messageRepository.GetPendingCounterOfferByNegotiationAsync(negotiation.NegotiationId, cancellationToken);
                    if (counterMessage is not null)
                    {
                        counterMessage.OfferStatus = (int)OfferStatus.Cancelled;
                        await _messageRepository.UpdateAsync(counterMessage, cancellationToken);
                    }
                }

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<OfferResponse>> AcceptAsync(
            Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            // Chỉ người nhận (chủ bài đăng) được accept
            if (offer.ReceiverId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != (int)OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offer.OfferId, cancellationToken);
            if (negotiation is null)
                return Result<OfferResponse>.Fail(NegotiationErrors.NotFound);

            if (negotiation.NegotiationStatus != (int)NegotiationStatus.Open)
                return Result<OfferResponse>.Fail(NegotiationErrors.NotOpen);

            var counterMessage = await _messageRepository.GetPendingCounterOfferByNegotiationAsync(negotiation.NegotiationId, cancellationToken);
            if (counterMessage is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            // Không được accept chính offer của mình
            if (counterMessage.SenderId == userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
            if (post is null)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);

            if (post.Status != (int)PostStatus.Active)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);

            if (offer.OfferQuantity > post.RemainingQuantity)
                return Result<OfferResponse>.Fail(
                    OfferErrors.QuantityExceedsRemaining(offer.OfferQuantity, post.RemainingQuantity));

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                counterMessage.OfferStatus = (int)OfferStatus.Accepted;
                await _messageRepository.UpdateAsync(counterMessage, cancellationToken);

                offer.OfferStatus = (int)OfferStatus.Accepted;
                await _offerRepository.UpdateAsync(offer, cancellationToken);

                negotiation.NegotiationStatus = (int)NegotiationStatus.Agreed;
                negotiation.FinalPrice = counterMessage.OfferPrice;
                negotiation.FinalQuantity = counterMessage.OfferQuantity;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                // Trừ RemainingQuantity và đóng post nếu hết hàng
                var newRemaining = post.RemainingQuantity - offer.OfferQuantity;
                post.RemainingQuantity = newRemaining < 0 ? 0 : newRemaining;
                if (post.RemainingQuantity == 0)
                    post.Status = (int)PostStatus.Closed;
                await _postRepository.UpdateAsync(post, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<OfferResponse>> RejectAsync(
            Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.ReceiverId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != (int)OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            var negotiation = await _negotiationRepository.GetByOfferIdAsync(offer.OfferId, cancellationToken);
            if (negotiation is null)
                return Result<OfferResponse>.Fail(NegotiationErrors.NotFound);

            if (negotiation.NegotiationStatus != (int)NegotiationStatus.Open)
                return Result<OfferResponse>.Fail(NegotiationErrors.NotOpen);

            var counterMessage = await _messageRepository.GetPendingCounterOfferByNegotiationAsync(negotiation.NegotiationId, cancellationToken);
            if (counterMessage is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            if (counterMessage.SenderId == userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                counterMessage.OfferStatus = (int)OfferStatus.Rejected;
                await _messageRepository.UpdateAsync(counterMessage, cancellationToken);

                offer.OfferStatus = (int)OfferStatus.Rejected;
                await _offerRepository.UpdateAsync(offer, cancellationToken);

                // Negotiation giữ Open để bên kia có thể gửi counter-offer mới

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }
    }
}
