using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Negotiates;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Negotiates;
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
        private readonly INegotiationRepository _negotiationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OfferService> _logger;
        private readonly IValidator<CreateOfferRequest> _createValidator;
        private readonly IValidator<UpdateOfferRequest> _updateValidator;
        private readonly IValidator<CounterInitialOfferRequest> _counterInitialValidator;
        private readonly IValidator<SendNegotiationCounterRequest> _negotiationCounterValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        private const decimal MinPriceFactor = 0.2m;
        private const decimal MaxPriceFactor = 3m;

        public OfferService(
            IOfferRepository offerRepository,
            INegotiationRepository negotiationRepository,
            IMessageRepository messageRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ILogger<OfferService> logger,
            IValidator<CreateOfferRequest> createValidator,
            IValidator<UpdateOfferRequest> updateValidator,
            IValidator<CounterInitialOfferRequest> counterInitialValidator,
            IValidator<SendNegotiationCounterRequest> negotiationCounterValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _offerRepository = offerRepository;
            _negotiationRepository = negotiationRepository;
            _messageRepository = messageRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _logger = logger;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _counterInitialValidator = counterInitialValidator;
            _negotiationCounterValidator = negotiationCounterValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // ================== GIAI ĐOẠN 1: NGOÀI NEGOTIATION ==================

        // Người mua gửi request từ Post. Không tạo Negotiation + Message
        public async Task<Result<OfferResponse>> CreateOfferAsync(Guid userId, CreateOfferRequest request, CancellationToken cancellationToken = default)
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
            offer.CreatedAt = DateTime.UtcNow;

            try
            {
                await _offerRepository.AddAsync(offer, cancellationToken);

                // Kiểm tra ngay trước SaveChangesAsync
                _logger.LogInformation("Creating offer for PostId: {PostId}", offer.PostId);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            // Unique partial index uq_offer_pending_post_sender chặn 2 request đồng thời
            // cùng tạo Offer Pending cho cùng (Post, Sender). Kẻ thua nhận 23505 -> trả lỗi nghiệp vụ.
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return Result<OfferResponse>.Fail(OfferErrors.DuplicatePending);
            }

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        // Người gửi chỉ được sửa giá và số lượng khi request ban đầu còn Pending
        public async Task<Result<OfferResponse>> UpdateAsync(Guid userId, Guid offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<OfferResponse>.Fail(ToValidationError(validation));

            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
            if (post is null)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotFound);

            if (post.Status != PostStatus.Active)
                return Result<OfferResponse>.Fail(OfferErrors.PostNotActive);

            if (request.OfferQuantity > post.RemainingQuantity)
                return Result<OfferResponse>.Fail(
                    OfferErrors.QuantityExceedsRemaining(
                        request.OfferQuantity,
                        post.RemainingQuantity));

            var priceError = ValidatePriceRange(post.BasePrice, request.OfferPrice);
            if (priceError is not null)
                return Result<OfferResponse>.Fail(priceError);

            offer.OfferPrice = request.OfferPrice;
            offer.OfferQuantity = request.OfferQuantity;

            _mapper.Map(request, offer);

            await _offerRepository.UpdateAsync(offer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        // Người gửi tự hủy request
        public async Task<Result<OfferResponse>> CancelAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            offer.OfferStatus = OfferStatus.Cancelled;

            await _offerRepository.UpdateAsync(offer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        // Người nhận từ chối request ban đầu
        public async Task<Result<OfferResponse>> RejectAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.ReceiverId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            if (offer.OfferStatus != OfferStatus.Pending)
                return Result<OfferResponse>.Fail(OfferErrors.NotPending);

            offer.OfferStatus = OfferStatus.Rejected;

            await _offerRepository.UpdateAsync(offer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        // Accept bên ngoài chỉ chấp nhận mở phiên thương lượng.
        // Chưa chốt giao dịch, chưa trừ tồn kho và chưa tạo AgreementForm.
        public async Task<Result<AcceptOfferResponse>> AcceptAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa dòng Offer (FOR UPDATE) TRONG transaction: serialize Accept/Counter/Cancel
                // đồng thời trên cùng một Offer, tránh 2 request cùng tạo Negotiation.
                var offer = await _offerRepository.GetByIdForUpdateAsync(offerId, cancellationToken);
                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.NotFound);
                }

                if (offer.ReceiverId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.Forbidden);
                }

                // Re-check trạng thái NGAY TRONG khóa: request thứ 2 sẽ thấy Accepted -> trả lỗi sạch
                // thay vì để DB unique index Negotiation_OfferId_key văng exception.
                if (offer.OfferStatus != OfferStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(OfferErrors.NotPending);
                }

                var post = await _postRepository.GetByIdAsync(offer.PostId, cancellationToken);
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

                if (offer.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AcceptOfferResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            offer.OfferQuantity,
                            post.RemainingQuantity));
                }

                var now = DateTime.UtcNow;
                var negotiation = CreateOpenNegotiation(offer, post, now);
                var initialOfferMessage = CreateInitialOfferMessage(
                    offer,
                    negotiation.NegotiationId,
                    post.BasePrice,
                    MessageOfferStatus.Pending,
                    now);

                // Accepted ở bảng Offer nghĩa là request mở thương lượng đã được nhận.
                // Kết quả giao dịch cuối cùng do NegotiationStatus quyết định.
                offer.OfferStatus = OfferStatus.Accepted;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _negotiationRepository.AddAsync(negotiation, cancellationToken);
                await _messageRepository.AddAsync(initialOfferMessage, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<AcceptOfferResponse>.Success(new AcceptOfferResponse
                {
                    OfferId = offer.OfferId,
                    NegotiationId = negotiation.NegotiationId,
                    OfferStatus = OfferStatus.Accepted,
                    NegotiationStatus = NegotiationStatus.Open
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // Người nhận counter request ban đầu -> mở Negotiation, lưu proposal gốc
        // thành Superseded và lưu counter mới thành Pending.
        public async Task<Result<NegotiationResponse>> CounterInitialOfferAsync(Guid userId, Guid offerId, CounterInitialOfferRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _counterInitialValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<NegotiationResponse>.Fail(ToValidationError(validation));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Khóa dòng Offer TRONG transaction: chống Accept + Counter đồng thời
                // trên cùng một Offer tạo ra 2 Negotiation.
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

                // Kiểm tra 1 OfferId -> tối đa 1 Negotiation
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

                // Giữ lịch sử request gốc trước khi đồng bộ Offer sang mức counter mới.
                var initialOfferMessage = CreateInitialOfferMessage(
                    offer,
                    negotiation.NegotiationId,
                    post.BasePrice,
                    MessageOfferStatus.Superseded,
                    now);

                var counterMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiation.NegotiationId,
                    SenderId = userId,
                    //MessageContent = request.MessageContent ?? "Đề nghị mức giá khác.",
                    MessageType = MessageType.CounterOffer,
                    OfferPrice = request.OfferPrice,
                    OfferQuantity = request.OfferQuantity,
                    OfferStatus = MessageOfferStatus.Pending,
                    MediaUrl = null,
                    BasePriceSnapshot = post.BasePrice,
                    IsRead = false,
                    CreatedAt = now
                };

                // Offer lưu snapshot mới + Messages giữ lịch sử thay đổi
                offer.OfferPrice = request.OfferPrice;
                offer.OfferQuantity = request.OfferQuantity;
                offer.OfferStatus = OfferStatus.Accepted;

                await _offerRepository.UpdateAsync(offer, cancellationToken);
                await _negotiationRepository.AddAsync(negotiation, cancellationToken);
                await _messageRepository.AddAsync(initialOfferMessage, cancellationToken);
                await _messageRepository.AddAsync(counterMessage, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<NegotiationResponse>.Success(
                    _mapper.Map<NegotiationResponse>(negotiation));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ================== GIAI ĐOẠN 2: TRONG NEGOTIATION ==================

        // Một bên gửi proposal mới. Không được tự counter đè lên proposal Pending
        // do chính mình vừa gửi. Proposal Pending của đối phương sẽ thành Superseded.
        public async Task<Result<NegotiationResponse>> SendNegotiationCounterAsync(Guid userId, Guid negotiationId, SendNegotiationCounterRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _negotiationCounterValidator.ValidateAsync(
                request,
                cancellationToken);

            if (!validation.IsValid)
                return Result<NegotiationResponse>.Fail(ToValidationError(validation));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {

                // Khóa dòng Negotiation: serialize 2 counter đồng thời trong cùng một negotiation.
                var negotiation = await _negotiationRepository.GetByIdForUpdateAsync(
                    negotiationId,
                    cancellationToken);

                if (negotiation is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                //Chỉ cho phép gửi counter khi trạng thái là Open hoặc Agreed
                if (negotiation.NegotiationStatus != NegotiationStatus.Open && negotiation.NegotiationStatus != NegotiationStatus.Agreed)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.InvalidStatusForCounter);
                }

                //nếu Seller/Buyer tiếp tục Counter sau khi Buyer đã Accept
                if (negotiation.NegotiationStatus == NegotiationStatus.Agreed)
                {
                    negotiation.NegotiationStatus = NegotiationStatus.Open;

                    negotiation.FinalPrice = null;
                    negotiation.FinalQuantity = null;

                    // Lúc này Seller sẽ mất quyền tạo form ở UI cho đến khi Buyer Accept lại
                }

                // Khóa dòng proposal Pending: chống 2 counter cùng lúc cùng supersede và
                // tạo ra 2 proposal Pending trong cùng negotiation.
                var pendingMessage =
                    await _messageRepository.GetPendingProposalForUpdateAsync(
                        negotiationId,
                        cancellationToken);

                if (pendingMessage is not null && pendingMessage.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                var post = await _postRepository.GetByIdAsync(
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

                var offer = await _offerRepository.GetByIdAsync(
                    negotiation.OfferId,
                    cancellationToken);

                if (offer is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotFound);
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
                    CreatedAt = now
                };

                if (pendingMessage is not null)
                {
                    pendingMessage.OfferStatus = MessageOfferStatus.Superseded;
                    await _messageRepository.UpdateAsync(pendingMessage, cancellationToken);
                }

                // Đồng bộ Offer về proposal mới nhất; lịch sử vẫn nằm trong Messages.
                offer.OfferPrice = request.OfferPrice;
                offer.OfferQuantity = request.OfferQuantity;
                await _offerRepository.UpdateAsync(offer, cancellationToken);

                await _messageRepository.AddAsync(counterMessage, cancellationToken);

                negotiation.LastMessageAt = now;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<NegotiationResponse>.Success(
                    _mapper.Map<NegotiationResponse>(negotiation));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // CHỈ Buyer mới được gọi hành động này, bất kể ai gửi proposal cuối cùng (kể cả khi
        // Seller là người vừa counter). Lý do: quyền "chốt deal" luôn thuộc về Buyer, tránh trường hợp
        // Seller tự ý quyết định giá mà không cần Buyer thật sự chấp thuận. Seller vẫn giữ quyền kiểm soát
        // riêng ở bước tạo Agreement Form sau đó (có thể không tạo nếu không đồng ý).
        public async Task<Result<NegotiationResponse>> AcceptNegotiationAsync(
            Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
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

                // Ràng buộc cứng: chỉ Buyer mới Accept được, không dùng IsParticipant chung nữa
                if (negotiation.BuyerId != userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                // Re-check trạng thái NGAY TRONG khóa.
                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotOpen);
                }

                var pendingMessage =
                    await _messageRepository.GetPendingProposalForUpdateAsync(
                        negotiationId,
                        cancellationToken);

                if (pendingMessage is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotPending);
                }

                // Vẫn giữ chặn: Buyer không thể tự Accept đề nghị do chính mình vừa gửi
                // (nếu proposal Pending hiện tại là do Buyer tự gửi, chưa có ai phản hồi thì chưa đủ điều kiện chốt)
                if (pendingMessage.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
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
                // để negotiation thứ 2 trừ dựa trên số liệu mới nhất, không dùng dữ liệu cũ.
                if (pendingMessage.OfferQuantity > post.RemainingQuantity)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(
                        OfferErrors.QuantityExceedsRemaining(
                            pendingMessage.OfferQuantity,
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

                pendingMessage.OfferStatus = MessageOfferStatus.Accepted;
                await _messageRepository.UpdateAsync(pendingMessage, cancellationToken);

                negotiation.NegotiationStatus = NegotiationStatus.Agreed;
                negotiation.FinalPrice = pendingMessage.OfferPrice;
                negotiation.FinalQuantity = pendingMessage.OfferQuantity;
                negotiation.LastMessageAt = DateTime.UtcNow;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                offer.OfferPrice = pendingMessage.OfferPrice;
                offer.OfferQuantity = pendingMessage.OfferQuantity;
                await _offerRepository.UpdateAsync(offer, cancellationToken);

                //DeductPostQuantity(post, pendingMessage.OfferQuantity); ////giảm số lượng hàng hoá trong bài đăng
                //await _postRepository.UpdateAsync(post, cancellationToken);

                // Agreement KHÔNG tạo tự động tại đây — Negotiation.Agreed chỉ là điều kiện đủ để
                // Seller chủ động tạo Agreement Form ở bước riêng sau đó (nghiệp vụ AgreementService).

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
        public async Task<Result<NegotiationResponse>> RejectNegotiationProposalAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
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
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotFound);
                }

                if (!IsParticipant(negotiation, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                if (negotiation.NegotiationStatus != NegotiationStatus.Open)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(NegotiationErrors.NotOpen);
                }

                // Khóa dòng proposal Pending để không reject đè lên proposal đã bị counter/supersede.
                var pendingMessage =
                    await _messageRepository.GetPendingProposalForUpdateAsync(
                        negotiationId,
                        cancellationToken);

                if (pendingMessage is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.NotPending);
                }

                if (pendingMessage.SenderId == userId)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<NegotiationResponse>.Fail(OfferErrors.Forbidden);
                }

                pendingMessage.OfferStatus = MessageOfferStatus.Rejected;
                await _messageRepository.UpdateAsync(pendingMessage, cancellationToken);

                // Không đổi NegotiationStatus và không đổi OfferStatus.
                negotiation.LastMessageAt = DateTime.UtcNow;
                await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<NegotiationResponse>.Success(
                    _mapper.Map<NegotiationResponse>(negotiation));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ================== QUERY ==================

        public async Task<Result<OfferResponse>> GetByIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var offer = await _offerRepository.GetByIdAsync(offerId, cancellationToken);
            if (offer is null)
                return Result<OfferResponse>.Fail(OfferErrors.NotFound);

            if (offer.SenderId != userId && offer.ReceiverId != userId)
                return Result<OfferResponse>.Fail(OfferErrors.Forbidden);

            return Result<OfferResponse>.Success(_mapper.Map<OfferResponse>(offer));
        }

        public async Task<Result<PagedResult<OfferResponse>>> GetSentAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetSentAsync(
                userId,
                request,
                cancellationToken);

            return Result<PagedResult<OfferResponse>>.Success(MapPaged(paged));
        }

        public async Task<Result<PagedResult<OfferResponse>>> GetReceivedAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var paged = await _offerRepository.GetReceivedAsync(
                userId,
                request,
                cancellationToken);

            return Result<PagedResult<OfferResponse>>.Success(MapPaged(paged));
        }

        public async Task<Result<NegotiationResponse>> GetNegotiationByIdAsync(Guid userId, Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetByIdAsync(
                negotiationId,
                cancellationToken);

            if (negotiation is null)
                return Result<NegotiationResponse>.Fail(
                    NegotiationErrors.NotFound);

            if (!IsParticipant(negotiation, userId))
                return Result<NegotiationResponse>.Fail(
                    OfferErrors.Forbidden);

            var response = _mapper.Map<NegotiationResponse>(negotiation);

            return Result<NegotiationResponse>.Success(response);
        }

        public async Task<Result<NegotiationResponse>> GetNegotiationByOfferIdAsync(Guid userId, Guid offerId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepository.GetByOfferIdAsync(
                offerId,
                cancellationToken);

            if (negotiation is null)
                return Result<NegotiationResponse>.Fail(
                    NegotiationErrors.NotFound);

            if (!IsParticipant(negotiation, userId))
                return Result<NegotiationResponse>.Fail(
                    OfferErrors.Forbidden);

            var response = _mapper.Map<NegotiationResponse>(negotiation);

            return Result<NegotiationResponse>.Success(response);
        }

        public async Task<Result<PagedResult<MessageResponse>>> GetNegotiationMessagesAsync(Guid userId, Guid negotiationId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            if (request.PageNumber < 1)
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    ValidationErrors.InvalidRequest(
                        "PageNumber phải lớn hơn hoặc bằng 1."));
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    ValidationErrors.InvalidRequest(
                        "PageSize phải nằm trong khoảng từ 1 đến 100."));
            }

            var negotiation = await _negotiationRepository.GetByIdAsync(
                negotiationId,
                cancellationToken);

            if (negotiation is null)
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    NegotiationErrors.NotFound);
            }

            if (!IsParticipant(negotiation, userId))
            {
                return Result<PagedResult<MessageResponse>>.Fail(
                    OfferErrors.Forbidden);
            }

            var paged = await _messageRepository.GetByNegotiationIdAsync(
                negotiationId,
                request,
                cancellationToken);

            var response = new PagedResult<MessageResponse>
            {
                Items = paged.Items
                    .Select(x => _mapper.Map<MessageResponse>(x))
                    .ToList(),

                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<MessageResponse>>.Success(response);
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

        private message CreateInitialOfferMessage(offer offer, Guid negotiationId, decimal? basePrice, MessageOfferStatus status, DateTime now)
        {
            var initialMessage = _mapper.Map<message>(offer);

            initialMessage.MessageId = Guid.NewGuid();
            initialMessage.NegotiationId = negotiationId;
            initialMessage.MessageContent = "Đề nghị thương lượng ban đầu";
            initialMessage.MessageType = MessageType.Offer;
            initialMessage.OfferStatus = status;
            initialMessage.MediaUrl = null;
            initialMessage.IsRead = true;
            initialMessage.BasePriceSnapshot = basePrice;
            initialMessage.CreatedAt = now;

            return initialMessage;
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

            //// Chỉ C (Personal) và B (Business) được tham gia Offer; Moderator/Admin không được.
            //if (sender.Role != UserRole.Personal && sender.Role != UserRole.Business)
            //    return OfferErrors.RoleNotAllowed;

            //// Bài đăng Bán (do Personal tạo): Personal hoặc Business khác được Offer mua.
            //// Bài đăng Mua (do Business tạo): chỉ Personal được Offer bán, chặn B2B.
            //if (post.PostType == PostType.Buy && sender.Role == UserRole.Business)
            //    return OfferErrors.BusinessCannotOfferBuyPost;

            return null;
        }

        private static bool IsParticipant(negotiation negotiation, Guid userId)
        {
            return negotiation.BuyerId == userId || negotiation.SellerId == userId;
        }

        //private static void DeductPostQuantity(post post, int quantity)
        //{
        //    post.RemainingQuantity -= quantity;

        //    if (post.RemainingQuantity <= 0)
        //    {
        //        post.RemainingQuantity = 0;
        //        post.Status = PostStatus.Closed;
        //    }
        //}

        private PagedResult<OfferResponse> MapPaged(PagedResult<offer> paged)
        {
            return new PagedResult<OfferResponse>
            {
                Items = paged.Items
                    .Select(x => _mapper.Map<OfferResponse>(x))
                    .ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
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
    }
}
