using HomeCycle.Application.Commons.Helpers;
using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Agreements
{
    public class AgreementFormService : IAgreementFormService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly INegotiationRepository _negotiationRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IConversationRepository _conversationRepo;
        private readonly IChatRealtimePublisher _chatRealtimePublisher;
        private readonly IMediaService _mediaService;
        private readonly IGhnService _ghnService;
        private readonly IMapper _mapper;
        private readonly ILogger<AgreementFormService> _logger;
        private readonly IPostRepository _postRepo;
        private readonly IOfferRepository _offerRepo;
        private readonly IProductRepository _productRepo;
        private readonly INotificationService _notificationService;
        private readonly IValidator<CreateAgreementFormRequest> _createValidator;
        private readonly IValidator<UpdateAgreementFormRequest> _updateValidator;
        private readonly IValidator<CalculateGhnFeeRequest> _shippingFeeValidator;
        private readonly IValidator<GhnShippingPreviewRequest> _shippingPreviewValidator;
        private readonly IValidator<AcceptAgreementRequest> _acceptValidator;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly HomeCycle.Application.Interfaces.Repositories.Profiles.IBusinessProfileRepository _businessProfileRepo;
        private readonly HomeCycle.Application.Interfaces.Repositories.Orders.IOrderRepository _orderRepo;
        private readonly HomeCycle.Application.Interfaces.Repositories.Users.IUserRepository _userRepo;
        private readonly HomeCycle.Application.Interfaces.Repositories.Users.IPersonalProfileRepository _profileRepo;


        public AgreementFormService(
            IUnitOfWork unitOfWork,
            IAgreementFormRepository agreementRepo,
            INegotiationRepository negotiationRepo,
            IMessageRepository messageRepo,
            IConversationRepository conversationRepo,
            IChatRealtimePublisher chatRealtimePublisher,
            IMediaService mediaService,
            IGhnService ghnService,
            IMapper mapper,
            ILogger<AgreementFormService> logger,
            IPostRepository postRepo,
            IOfferRepository offerRepo,
            IProductRepository productRepo,
            INotificationService notificationService,
            IValidator<CreateAgreementFormRequest> createValidator,
            IValidator<UpdateAgreementFormRequest> updateValidator,
            IValidator<CalculateGhnFeeRequest> shippingFeeValidator,
            IValidator<GhnShippingPreviewRequest> shippingPreviewValidator,
            IValidator<AcceptAgreementRequest> acceptValidator,
            HomeCycle.Application.Interfaces.Repositories.Users.IUserRepository userRepo,
            HomeCycle.Application.Interfaces.Repositories.Users.IPersonalProfileRepository profileRepo,
            HomeCycle.Application.Interfaces.Repositories.Orders.IOrderRepository orderRepo,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            HomeCycle.Application.Interfaces.Repositories.Profiles.IBusinessProfileRepository businessProfileRepo)
        {
            _unitOfWork = unitOfWork;
            _agreementRepo = agreementRepo;
            _negotiationRepo = negotiationRepo;
            _messageRepo = messageRepo;
            _conversationRepo = conversationRepo;
            _chatRealtimePublisher = chatRealtimePublisher;
            _mediaService = mediaService;
            _ghnService = ghnService;
            _mapper = mapper;
            _logger = logger;
            _postRepo = postRepo;
            _offerRepo = offerRepo;
            _productRepo = productRepo;
            _notificationService = notificationService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _shippingFeeValidator = shippingFeeValidator;
            _shippingPreviewValidator = shippingPreviewValidator;
            _acceptValidator = acceptValidator;
            _userRepo = userRepo;
            _profileRepo = profileRepo;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _businessProfileRepo = businessProfileRepo;
        }

        public async Task<Result<AgreementPreviewResponse>> GetPreviewAsync(Guid negotiationId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepo.GetByIdAsync(negotiationId, cancellationToken);
            if (negotiation == null)
                return Result<AgreementPreviewResponse>.Fail(new Error("Negotiation.NotFound", "Không tìm thấy cuộc thương lượng."));

            bool isSeller = negotiation.SellerId == currentUserId;
            bool isBuyer = negotiation.BuyerId == currentUserId;

            if (!isSeller && !isBuyer)
                return Result<AgreementPreviewResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền truy cập."));

            var agreement = await _agreementRepo.GetByNegotiationIdAsync(negotiationId, cancellationToken);

            var response = new AgreementPreviewResponse
            {
                NegotiationId = negotiationId,
                UserRole = isSeller ? "Seller" : "Buyer",
                HasAgreement = agreement != null
            };

            if (agreement == null)
            {
                response.CanCreate = isSeller;
                response.CanEdit = false;
                response.CanConfirm = false;
            }
            else
            {
                response.AgreementId = agreement.AgreementId;
                bool isPending = agreement.AgreementStatus == (int)AgreementStatus.Pending;


                response.CanEdit = isPending;
                if (isPending)
                {
                    response.CanConfirm = isSeller ? agreement.SellerConfirmedAt == null : agreement.BuyerConfirmedAt == null;
                }
            }

            return Result<AgreementPreviewResponse>.Success(response);
        }


        public async Task<Result<Guid>> CreateAgreementAsync(CreateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));

                return Result<Guid>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            // Giao hàng qua GHN: server tự gọi lại API tính phí để con số trong hợp đồng luôn chính xác
            if (GhnShippingCalculationHelper.IsAgreementGhnDelivery(request.AgreementType, request.AgreementDetails?.DeliveryMethod))
            {
                var authorized = await GetAuthorizedNegotiationAsync(request.NegotiationId, currentUserId, cancellationToken);
                if (!authorized.IsSuccess) return Result<Guid>.Fail(authorized.Error!);
                var negotiation = authorized.Data!;
                if (negotiation.SellerId != currentUserId)
                    return Result<Guid>.Fail(new Error("Auth.Forbidden", "Chỉ người bán được tạo thỏa thuận."));
                if (negotiation.NegotiationStatus != NegotiationStatus.Agreed || negotiation.FinalQuantity is not > 0 || negotiation.FinalPrice is not > 0)
                    return Result<Guid>.Fail(new Error("Agreement.TermsNotAgreed", "Cần thống nhất giá và số lượng trước khi gọi GHN."));
                if (await _agreementRepo.GetByNegotiationIdAsync(request.NegotiationId, cancellationToken) != null)
                    return Result<Guid>.Fail(new Error("Agreement.AlreadyExists", "Thỏa thuận đã tồn tại."));

                var feeResult = await ComputeShippingFeeAsync(request.AgreementDetails, negotiation.PostId, request.NegotiationId, cancellationToken);
                if (!feeResult.IsSuccess)
                    return Result<Guid>.Fail(feeResult.Error!);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var contextSnapshot = await _negotiationRepo.GetByIdAsync(request.NegotiationId, cancellationToken);
                if (contextSnapshot != null) await _postRepo.LockAsync(contextSnapshot.PostId, contextSnapshot.Offer?.BuyPostId, cancellationToken);
                var negotiation = await _negotiationRepo.GetByIdForUpdateAsync(request.NegotiationId, cancellationToken);
                if (negotiation == null)
                    return Result<Guid>.Fail(new Error("Negotiation.NotFound", "Không tìm thấy cuộc thương lượng."));

                if (negotiation.SellerId != currentUserId)
                    return Result<Guid>.Fail(new Error("Auth.Forbidden", "Chỉ người bán mới có quyền tạo thỏa thuận."));

                var existingAgreement = await _agreementRepo.GetByNegotiationIdAsync(request.NegotiationId, cancellationToken);
                if (existingAgreement != null)
                    return Result<Guid>.Fail(new Error("Agreement.AlreadyExists", "Thỏa thuận đã tồn tại."));

                var post = await _postRepo.GetByIdAsync(negotiation.PostId, cancellationToken);
                if (post == null)
                    return Result<Guid>.Fail(new Error("Post.NotFound", "Bài đăng không tồn tại."));


                var offer = await _offerRepo.GetByIdAsync(negotiation.OfferId, cancellationToken);
                if (offer == null)
                    return Result<Guid>.Fail(new Error("Offer.NotFound", "Không tìm thấy Offer ban đầu."));

                if (negotiation.NegotiationStatus != NegotiationStatus.Agreed || negotiation.FinalQuantity is not > 0 || negotiation.FinalPrice is not > 0)
                    return Result<Guid>.Fail(new Error("Agreement.TermsNotAgreed", "Cần thống nhất giá và số lượng trước khi tạo thỏa thuận."));
                var capacityError = await _postRepo.ValidateCapacityAsync(offer, negotiation.FinalQuantity.Value, negotiation.NegotiationId, false, cancellationToken);
                if (capacityError != null) return Result<Guid>.Fail(capacityError);
                var snapshotResult = await BuildProductSnapshotAsync(post.PostId, cancellationToken);
                if (!snapshotResult.IsSuccess)
                    return Result<Guid>.Fail(snapshotResult.Error!);

                var now = DateTime.UtcNow;
                var conversation = await GetOrCreateConversationAsync(negotiation, now, cancellationToken);

                var newAgreement = new agreement_form
                {
                    AgreementId = Guid.NewGuid(),
                    NegotiationId = request.NegotiationId,
                    PostId = negotiation.PostId,
                    SellerId = negotiation.SellerId,
                    BuyerId = negotiation.BuyerId,


                    InitialPrice = offer.OfferPrice,
                    FinalPrice = negotiation.FinalPrice,
                    Quantity = negotiation.FinalQuantity!.Value,

                    AgreementType = (int)request.AgreementType,
                    PaymentType = (int)request.PaymentType,
                    AgreementStatus = (int)AgreementStatus.Pending,

                    PSnapshot = JsonSerializer.Serialize(snapshotResult.Data),
                    AgreementDetailsJsonb = JsonSerializer.Serialize(request.AgreementDetails),

                    CreatedAt = DateTime.UtcNow,
                    BuyerConfirmedAt = null,
                    SellerConfirmedAt = DateTime.UtcNow
                };

                negotiation.NegotiationStatus = NegotiationStatus.AgreementPending;

                var agreementMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    ConversationId = conversation.ConversationId,
                    NegotiationId = request.NegotiationId,
                    SenderId = negotiation.SellerId,        // người tạo = seller -> hiện bên phải
                    MessageType = MessageType.Agreement,
                    MessageContent = "Đã tạo thỏa thuận mua bán, vui lòng kiểm tra và xác nhận.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                negotiation.LastMessageAt = now;

                await _agreementRepo.AddAsync(newAgreement, cancellationToken);
                await _negotiationRepo.UpdateAsync(negotiation, cancellationToken);
                await _messageRepo.AddAsync(agreementMessage, cancellationToken);
                await _conversationRepo.UpdateLastActivityAsync(conversation.ConversationId, now, cancellationToken);

                var agreementNotification = await AddAgreementNotificationPendingAsync(
                    negotiation.BuyerId,
                    "Có thỏa thuận mới",
                    "Người bán vừa tạo thỏa thuận mua bán. Vui lòng kiểm tra và xác nhận.",
                    newAgreement.AgreementId,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = _mapper.Map<MessageResponse>(agreementMessage);
                await PublishChatActivitySafelyAsync(negotiation, response);
                await _notificationService.PublishCreatedSafelyAsync(agreementNotification);

                return Result<Guid>.Success(newAgreement.AgreementId);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }


        public async Task<Result<AgreementDetailResponse>> GetDetailAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<AgreementDetailResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.SellerId != currentUserId && agreement.BuyerId != currentUserId)
                return Result<AgreementDetailResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền xem thỏa thuận này."));

            var details = string.IsNullOrEmpty(agreement.AgreementDetailsJsonb)
                ? null
                : JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb);

            decimal basePrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
            decimal estimatedShippingFee = details?.EstimatedShippingFee ?? 0;

            var response = new AgreementDetailResponse
            {
                AgreementId = agreement.AgreementId,
                NegotiationId = agreement.NegotiationId,
                PostId = agreement.PostId,
                SellerId = agreement.SellerId,
                BuyerId = agreement.BuyerId,
                InitialPrice = agreement.InitialPrice ?? 0,
                FinalPrice = agreement.FinalPrice ?? 0,
                Quantity = agreement.Quantity,
                AgreementType = (AgreementType)agreement.AgreementType,
                PaymentType = (PaymentType)agreement.PaymentType,
                AgreementStatus = (AgreementStatus)agreement.AgreementStatus,
                BuyerConfirmedAt = agreement.BuyerConfirmedAt,
                SellerConfirmedAt = agreement.SellerConfirmedAt,
                CreatedAt = agreement.CreatedAt,
                AgreementDetails = details,
                EstimatedShippingFee = estimatedShippingFee,
                TotalAmount = basePrice + estimatedShippingFee
            };

            return Result<AgreementDetailResponse>.Success(response);
        }

        public async Task<Result<AgreementActionResponse>> UpdateAgreementAsync(Guid agreementId, UpdateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<AgreementActionResponse>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            var agreementSnapshot = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreementSnapshot == null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            bool isSeller = agreementSnapshot.SellerId == currentUserId;
            bool isBuyer = agreementSnapshot.BuyerId == currentUserId;
            if (!isSeller && !isBuyer)
                return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền cập nhật thỏa thuận này."));

            if (agreementSnapshot.AgreementStatus != (int)AgreementStatus.Pending)
            {
                return Result<AgreementActionResponse>.Fail(new Error(
                    "Agreement.InvalidStatus",
                    "Thỏa thuận đã được cả hai bên chốt. Vui lòng yêu cầu mở lại (Request Edit) trước khi chỉnh sửa."));
            }

            if (GhnShippingCalculationHelper.IsAgreementGhnDelivery(request.AgreementType, request.AgreementDetails?.DeliveryMethod))
            {
                var authorized = await GetAuthorizedNegotiationAsync(agreementSnapshot.NegotiationId, currentUserId, cancellationToken);
                if (!authorized.IsSuccess) return Result<AgreementActionResponse>.Fail(authorized.Error!);
                var feeResult = await ComputeShippingFeeAsync(request.AgreementDetails, agreementSnapshot.PostId, agreementSnapshot.NegotiationId, cancellationToken);
                if (!feeResult.IsSuccess)
                    return Result<AgreementActionResponse>.Fail(feeResult.Error!);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var tradeSnapshot = await _postRepo.GetTradeByAgreementAsync(agreementId, cancellationToken);
                if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                var agreement = await _agreementRepo.GetByIdForUpdateAsync(agreementId, cancellationToken);
                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));
                }

                isSeller = agreement.SellerId == currentUserId;
                isBuyer = agreement.BuyerId == currentUserId;
                if (!isSeller && !isBuyer)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền cập nhật thỏa thuận này."));
                }

                if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error(
                        "Agreement.InvalidStatus",
                        "Thỏa thuận đã được cả hai bên chốt. Vui lòng yêu cầu mở lại (Request Edit) trước khi chỉnh sửa."));
                }

                var currentRevision = 1;
                if (!string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                {
                    var currentDetails = JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb);
                    if (currentDetails != null)
                        currentRevision = currentDetails.Revision;
                }

                if (request.AgreementDetails != null)
                {
                    request.AgreementDetails = new AgreementDetailsDto
                    {
                        Revision = currentRevision + 1,
                        Notes = request.AgreementDetails.Notes,
                        InspectionDate = request.AgreementDetails.InspectionDate,
                        InspectionAddress = request.AgreementDetails.InspectionAddress,
                        CollectionDate = request.AgreementDetails.CollectionDate,
                        PickupAddress = request.AgreementDetails.PickupAddress,
                        DeliveryAddress = request.AgreementDetails.DeliveryAddress,
                        DeliveryMethod = request.AgreementDetails.DeliveryMethod,
                        GhnInfo = request.AgreementDetails.GhnInfo,
                        CodValue = request.AgreementDetails.CodValue,
                        EstimatedShippingFee = request.AgreementDetails.EstimatedShippingFee
                    };
                }

                agreement.AgreementType = (int)request.AgreementType;
                agreement.PaymentType = (int)request.PaymentType;
                agreement.AgreementDetailsJsonb = JsonSerializer.Serialize(request.AgreementDetails);

                var now = DateTime.UtcNow;
                if (isSeller)
                {
                    agreement.SellerConfirmedAt = now;
                    agreement.BuyerConfirmedAt = null;
                }
                else
                {
                    agreement.BuyerConfirmedAt = now;
                    agreement.SellerConfirmedAt = null;
                }

                var negotiation = await _negotiationRepo.GetByIdForUpdateAsync(agreement.NegotiationId, cancellationToken);
                if (negotiation == null)
                    throw new InvalidOperationException("Không tìm thấy cuộc thương lượng của thỏa thuận.");

                var conversation = await GetOrCreateConversationAsync(negotiation, now, cancellationToken);
                var actorRole = isSeller ? "Người bán" : "Người mua";
                var agreementMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiation.NegotiationId,
                    ConversationId = conversation.ConversationId,
                    SenderId = currentUserId,
                    MessageType = MessageType.Agreement,
                    MessageContent = $"{actorRole} đã cập nhật thỏa thuận. Bên còn lại cần kiểm tra và xác nhận lại.",
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                negotiation.LastMessageAt = now;

                var updateRecipientId = isSeller ? agreement.BuyerId : agreement.SellerId;

                await _agreementRepo.UpdateAsync(agreement, cancellationToken);
                await _negotiationRepo.UpdateAsync(negotiation, cancellationToken);
                await _messageRepo.AddAsync(agreementMessage, cancellationToken);
                await _conversationRepo.UpdateLastActivityAsync(conversation.ConversationId, now, cancellationToken);


                var agreementNotification = await AddAgreementNotificationPendingAsync(
                    updateRecipientId,
                    "Thỏa thuận vừa được cập nhật",
                    $"{actorRole} đã cập nhật thỏa thuận. Vui lòng kiểm tra và xác nhận lại nội dung mới.",
                    agreement.AgreementId,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await PublishChatActivitySafelyAsync(negotiation, _mapper.Map<MessageResponse>(agreementMessage));


                await _notificationService.PublishCreatedSafelyAsync(agreementNotification);


                return Result<AgreementActionResponse>.Success(new AgreementActionResponse
                {
                    Message = "Cập nhật thỏa thuận thành công. Bên còn lại cần xác nhận lại nội dung mới.",
                    AgreementId = agreement.AgreementId,
                    AgreementStatus = (AgreementStatus)agreement.AgreementStatus,
                    SellerConfirmed = agreement.SellerConfirmedAt != null,
                    BuyerConfirmed = agreement.BuyerConfirmedAt != null,
                    SellerConfirmedAt = agreement.SellerConfirmedAt,
                    BuyerConfirmedAt = agreement.BuyerConfirmedAt
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }


        public async Task<Result<AgreementActionResponse>> AcceptAgreementAsync(Guid agreementId, Guid currentUserId, int expectedRevision, CancellationToken cancellationToken = default)
        {
            var validationResult = await _acceptValidator.ValidateAsync(
                new AcceptAgreementRequest { ExpectedRevision = expectedRevision },
                cancellationToken);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<AgreementActionResponse>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var tradeSnapshot = await _postRepo.GetTradeByAgreementAsync(agreementId, cancellationToken);
                if (tradeSnapshot != null) await _postRepo.LockAsync(tradeSnapshot.PostId, tradeSnapshot.BuyPostId, cancellationToken);
                var agreement = await _agreementRepo.GetByIdForUpdateAsync(agreementId, cancellationToken);
                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));
                }

                bool isSeller = agreement.SellerId == currentUserId;
                bool isBuyer = agreement.BuyerId == currentUserId;
                if (!isSeller && !isBuyer)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền chấp nhận thỏa thuận này."));
                }

                if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.InvalidStatus", "Thỏa thuận không ở trạng thái chờ xác nhận."));
                }

                if ((isSeller && agreement.SellerConfirmedAt != null) || (isBuyer && agreement.BuyerConfirmedAt != null))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.AlreadyConfirmed", "Bạn đã xác nhận thỏa thuận này rồi."));
                }

                var currentDetails = string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb)
                    ? null
                    : JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb);
                var actualRevision = currentDetails?.Revision ?? 1;

                if (actualRevision != expectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<AgreementActionResponse>.Fail(new Error(
                        "Agreement.RevisionMismatch",
                        "Nội dung thỏa thuận vừa được cập nhật. Vui lòng tải lại và xem nội dung mới nhất trước khi xác nhận."));
                }

                var now = DateTime.UtcNow;
                if (isSeller)
                    agreement.SellerConfirmedAt = now;
                else
                    agreement.BuyerConfirmedAt = now;

                bool bothConfirmed = agreement.SellerConfirmedAt != null && agreement.BuyerConfirmedAt != null;
                if (bothConfirmed)
                    agreement.AgreementStatus = (int)AgreementStatus.Awaiting_Payment;

                var negotiation = await _negotiationRepo.GetByIdForUpdateAsync(agreement.NegotiationId, cancellationToken);
                if (negotiation == null)
                    throw new InvalidOperationException("Không tìm thấy cuộc thương lượng của thỏa thuận.");

                var conversation = await GetOrCreateConversationAsync(negotiation, now, cancellationToken);
                var actorRole = isSeller ? "Người bán" : "Người mua";
                var messageContent = bothConfirmed
                    ? "Cả hai bên đã xác nhận thỏa thuận. Người mua có thể tiến hành thanh toán."
                    : $"{actorRole} đã xác nhận thỏa thuận. Đang chờ bên còn lại xác nhận.";

                var agreementMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = negotiation.NegotiationId,
                    ConversationId = conversation.ConversationId,
                    SenderId = currentUserId,
                    MessageType = MessageType.Agreement,
                    MessageContent = messageContent,
                    IsRead = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                negotiation.LastMessageAt = now;

                var acceptRecipientId = isSeller ? agreement.BuyerId : agreement.SellerId;
                var acceptTitle = bothConfirmed ? "Thỏa thuận đã được chốt" : "Thỏa thuận vừa được xác nhận";
                var acceptMessage = bothConfirmed
                    ? (acceptRecipientId == agreement.BuyerId
                        ? "Cả hai bên đã đồng ý thỏa thuận. Vui lòng tiến hành thanh toán."
                        : "Cả hai bên đã đồng ý thỏa thuận.")
                    : $"{actorRole} đã xác nhận thỏa thuận. Đang chờ bạn xác nhận.";

                await _agreementRepo.UpdateAsync(agreement, cancellationToken);
                await _negotiationRepo.UpdateAsync(negotiation, cancellationToken);
                await _messageRepo.AddAsync(agreementMessage, cancellationToken);
                await _conversationRepo.UpdateLastActivityAsync(conversation.ConversationId, now, cancellationToken);

                var agreementNotification = await AddAgreementNotificationPendingAsync(
                    acceptRecipientId,
                    acceptTitle,
                    acceptMessage,
                    agreement.AgreementId,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await PublishChatActivitySafelyAsync(negotiation, _mapper.Map<MessageResponse>(agreementMessage));

                // Bên còn lại (không phải người vừa xác nhận) là người cần được báo.
                await _notificationService.PublishCreatedSafelyAsync(agreementNotification);


                return Result<AgreementActionResponse>.Success(new AgreementActionResponse
                {
                    Message = bothConfirmed
                        ? "Cả hai bên đã đồng ý. Vui lòng tiến hành thanh toán."
                        : "Bạn đã xác nhận thỏa thuận. Đang chờ bên còn lại xác nhận.",
                    AgreementId = agreement.AgreementId,
                    AgreementStatus = (AgreementStatus)agreement.AgreementStatus,
                    SellerConfirmed = agreement.SellerConfirmedAt != null,
                    BuyerConfirmed = agreement.BuyerConfirmedAt != null,
                    SellerConfirmedAt = agreement.SellerConfirmedAt,
                    BuyerConfirmedAt = agreement.BuyerConfirmedAt
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result<AgreementActionResponse>> RequestEditAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var trade = await _postRepo.GetTradeByAgreementAsync(agreementId, cancellationToken);
                if (trade != null) await _postRepo.LockAsync(trade.PostId, trade.BuyPostId, cancellationToken);
                var agreement = await _agreementRepo.GetByIdForUpdateAsync(agreementId, cancellationToken);
                if (agreement == null)
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));
                if (agreement.SellerId != currentUserId && agreement.BuyerId != currentUserId)
                    return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền chỉnh sửa thỏa thuận này."));
                // Serialize reopening against payment so a fulfilled agreement cannot reserve stock again.
                if (agreement.AgreementStatus != (int)AgreementStatus.Awaiting_Payment)
                    return Result<AgreementActionResponse>.Fail(new Error("Agreement.InvalidStatus", "Chỉ mở lại thỏa thuận đang chờ thanh toán."));
                agreement.AgreementStatus = (int)AgreementStatus.Pending;
                agreement.SellerConfirmedAt = null;
                agreement.BuyerConfirmedAt = null;
                await _agreementRepo.UpdateAsync(agreement, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<AgreementActionResponse>.Success(new AgreementActionResponse
                {
                    Message = "Đã mở lại thỏa thuận. Cả hai bên cần xác nhận lại sau khi cập nhật.",
                    AgreementId = agreement.AgreementId,
                    AgreementStatus = (AgreementStatus)agreement.AgreementStatus,
                    SellerConfirmed = false,
                    BuyerConfirmed = false
                });
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }

        public async Task<Result<PagedResult<PendingAgreementListItemDto>>> GetPendingPaymentAsync(
            Guid buyerId, PendingAgreementSearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _agreementRepo.GetPendingPaymentByBuyerAsync(buyerId, request, cancellationToken);
            return Result<PagedResult<PendingAgreementListItemDto>>.Success(result);
        }


        // ================== HELPER =====================
        private Task<notification> AddAgreementNotificationPendingAsync(
            Guid recipientId,
            string title,
            string message,
            Guid agreementId,
            CancellationToken cancellationToken)
        {
            return _notificationService.AddPendingAsync(
                new CreateNotificationCommand(
                    recipientId,
                    title,
                    message,
                    NotificationTargetType.Agreement,
                    agreementId),
                cancellationToken);
        }

        private async Task<conversation> GetOrCreateConversationAsync(
            negotiation negotiation,
            DateTime activityAt,
            CancellationToken cancellationToken)
        {
            conversation? conversation = null;

            if (negotiation.ConversationId.HasValue)
            {
                conversation = await _conversationRepo.GetByIdAsync(
                    negotiation.ConversationId.Value,
                    cancellationToken);
            }

            if (conversation != null)
                return conversation;

            conversation = await _conversationRepo.GetOrCreateAsync(
                negotiation.SellerId,
                negotiation.BuyerId,
                activityAt,
                cancellationToken);

            negotiation.ConversationId = conversation.ConversationId;
            return conversation;
        }

        //private async Task SendAgreementNotificationSafelyAsync(
        //    Guid recipientId,
        //    string title,
        //    string message,
        //    Guid agreementId,
        //    CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var notification = await _notificationService.AddPendingAsync(
        //            new CreateNotificationCommand(
        //                recipientId,
        //                title,
        //                message,
        //                NotificationTargetType.Agreement,
        //                agreementId),
        //            cancellationToken);

        //        await _notificationService.PublishCreatedSafelyAsync(notification);
        //    }
        //    catch (Exception exception)
        //    {
        //        _logger.LogWarning(
        //            exception,
        //            "Không thể tạo/phát notification cho AgreementId {AgreementId}, UserId {RecipientId}.",
        //            agreementId,
        //            recipientId);
        //    }
        //}

        private async Task PublishChatActivitySafelyAsync(
            negotiation negotiation,
            MessageResponse response)
        {
            await PublishMessageCreatedSafelyAsync(
                negotiation.NegotiationId,
                response);

            if (!negotiation.ConversationId.HasValue)
                return;

            await PublishConversationMessageCreatedSafelyAsync(
                negotiation.ConversationId.Value,
                response);

            await PublishConversationUpdatedSafelyAsync(
                negotiation,
                response);
        }

        private async Task PublishConversationMessageCreatedSafelyAsync(
            Guid conversationId,
            MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _chatRealtimePublisher.PublishConversationMessageCreatedAsync(
                    conversationId,
                    response,
                    timeout.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể phát ConversationMessageCreated cho MessageId {MessageId}.",
                    response.MessageId);
            }
        }

        private async Task PublishConversationUpdatedSafelyAsync(
            negotiation negotiation,
            MessageResponse lastMessage)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var conversationId = negotiation.ConversationId!.Value;

                var unreadDetails = await _messageRepo.GetUnreadCountsDetailAsync(
                    conversationId,
                    negotiation.SellerId,
                    negotiation.BuyerId,
                    timeout.Token);

                var conversationUnread = unreadDetails.ToDictionary(
                    x => x.Key,
                    x => x.Value.TotalConversationUnread);

                var negotiationUnread = unreadDetails.ToDictionary(
                    x => x.Key,
                    x => x.Value.UnreadByNegotiation.ToDictionary(
                        y => y.Key,
                        y => (int?)y.Value));

                await _chatRealtimePublisher.PublishConversationUpdatedAsync(
                    new[] { negotiation.SellerId, negotiation.BuyerId },
                    new ConversationUpdatedResponse
                    {
                        ConversationId = conversationId,
                        NegotiationId = negotiation.NegotiationId,
                        LastSenderId = lastMessage.SenderId,
                        LastMessagePreview = lastMessage.MessageContent ?? "[Thỏa thuận]",
                        LastMessageType = lastMessage.MessageType,
                        LastMessageAt = lastMessage.CreatedAt,
                        CurrentOfferPrice = negotiation.FinalPrice ?? negotiation.Offer?.OfferPrice,
                        CurrentOfferQuantity = negotiation.FinalQuantity ?? negotiation.Offer?.OfferQuantity ?? 0,
                        CurrentOfferVersion = negotiation.Offer?.Version,
                        NegotiationStatus = negotiation.NegotiationStatus ?? NegotiationStatus.Open,
                        ConversationUnreadByUser = conversationUnread,
                        NegotiationUnreadByUser = negotiationUnread
                    },
                    timeout.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Không thể phát ConversationUpdated cho NegotiationId {NegotiationId}.",
                    negotiation.NegotiationId);
            }
        }
        private async Task PublishMessageCreatedSafelyAsync(Guid negotiationId, MessageResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _chatRealtimePublisher.PublishMessageCreatedAsync(negotiationId, response, timeout.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể phát MessageCreated cho MessageId {MessageId}.", response.MessageId);
            }
        }

        public async Task<Result<ShippingFeePreviewResponse>>PreviewShippingFeeAsync(Guid negotiationId, Guid currentUserId, CalculateGhnFeeRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var authorized = await GetAuthorizedNegotiationAsync(negotiationId, currentUserId, cancellationToken);
            if (!authorized.IsSuccess) return Result<ShippingFeePreviewResponse>.Fail(authorized.Error!);
            var contextError = await ValidateAgreementGhnContextAsync(negotiationId, request.AgreementType, request.DeliveryMethod, cancellationToken);
            if (contextError != null) return Result<ShippingFeePreviewResponse>.Fail(contextError);

            var negotiation = await _negotiationRepo.GetByIdAsync(
                negotiationId,
                cancellationToken);

            if (negotiation is null)
            {
                return Result<ShippingFeePreviewResponse>.Fail(
                    new Error(
                        "Negotiation.NotFound",
                        "Không tìm thấy cuộc thương lượng."));
            }

            var isSeller = negotiation.SellerId == currentUserId;
            var isBuyer = negotiation.BuyerId == currentUserId;

            if (!isSeller && !isBuyer)
            {
                return Result<ShippingFeePreviewResponse>.Fail(
                    new Error(
                        "Auth.Forbidden",
                        "Bạn không có quyền tính phí vận chuyển cho cuộc thương lượng này."));
            }

            if (negotiation.NegotiationStatus == NegotiationStatus.Cancelled)
            {
                return Result<ShippingFeePreviewResponse>.Fail(
                    new Error(
                        "Negotiation.Cancelled",
                        "Không thể tính phí cho cuộc thương lượng đã bị hủy."));
            }

            var validationResult = await _shippingFeeValidator.ValidateAsync(
                request,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(
                    ", ",
                    validationResult.Errors
                        .Select(x => x.ErrorMessage)
                        .Distinct());

                return Result<ShippingFeePreviewResponse>.Fail(
                    new Error(
                        "ShippingFee.InvalidRequest",
                        errorMessage));
            }

            var quote = await _ghnService.GetShippingFeeAsync(
                request,
                cancellationToken);

            var response = new ShippingFeePreviewResponse
            {
                NegotiationId = negotiationId,
                ServiceTypeId = request.ServiceTypeId,
                EstimatedShippingFee = quote.TotalFee,

                Breakdown = new ShippingFeeBreakdownResponse
                {
                    ServiceFee = quote.Breakdown.ServiceFee,
                    InsuranceFee = quote.Breakdown.InsuranceFee,
                    PickStationFee = quote.Breakdown.PickStationFee,
                    CouponValue = quote.Breakdown.CouponValue,
                    R2sFee = quote.Breakdown.R2sFee,
                    DocumentReturnFee = quote.Breakdown.DocumentReturnFee,
                    DoubleCheckFee = quote.Breakdown.DoubleCheckFee,
                    CodFee = quote.Breakdown.CodFee,
                    PickRemoteAreasFee =
                        quote.Breakdown.PickRemoteAreasFee,
                    DeliverRemoteAreasFee =
                        quote.Breakdown.DeliverRemoteAreasFee,
                    CodFailedFee = quote.Breakdown.CodFailedFee
                }
            };

            return Result<ShippingFeePreviewResponse>.Success(response);
        }

        private const int GhnLightServiceTypeId = 2;
        private const int GhnHeavyServiceTypeId = 5;
        private const long HeavyParcelThresholdGram = 20_000L;

        public async Task<Result<GhnParcelInfoResponse>> GetGhnParcelInfoAsync(Guid negotiationId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var negotiationResult = await GetAuthorizedNegotiationAsync(negotiationId, currentUserId, cancellationToken);
            if (!negotiationResult.IsSuccess)
                return Result<GhnParcelInfoResponse>.Fail(negotiationResult.Error!);

            var product = await _productRepo.GetDetailByPostIdAsync(negotiationResult.Data!.PostId, cancellationToken);
            if (product is null)
                return Result<GhnParcelInfoResponse>.Fail(new Error("Product.NotFound", "Không tìm thấy sản phẩm của bài đăng."));

            var hasDimensions = TryNormalizeProductMeasurements(
                product,
                out var weightGram,
                out var lengthCm,
                out var widthCm,
                out var heightCm);

            var quantity = await GetOfferQuantityAsync(negotiationResult.Data!, cancellationToken);

            var totalWeightGram = checked((long)weightGram * quantity);
            if (quantity <= 0 || totalWeightGram > int.MaxValue)
                return Result<GhnParcelInfoResponse>.Fail(new Error("Ghn.ParcelInformationRequired", "Số lượng hoặc tổng khối lượng không hợp lệ."));

            var isHeavyParcel =
                totalWeightGram >= HeavyParcelThresholdGram;

            var serviceTypeId = isHeavyParcel
                ? GhnHeavyServiceTypeId
                : GhnLightServiceTypeId;

            var response = new GhnParcelInfoResponse
            {
                Sender = await GetContactDefaultsAsync(negotiationResult.Data!.SellerId, cancellationToken),
                Receiver = await GetContactDefaultsAsync(negotiationResult.Data!.BuyerId, cancellationToken),
                NegotiationId = negotiationId,
                ServiceTypeId = serviceTypeId,
                HasProductDimensions = hasDimensions,
                RequiresPackagingDimensions = !hasDimensions || quantity > 1,
                LightParcel = hasDimensions && !isHeavyParcel ? new GhnLightParcelSnapshotDto
                    {
                        WeightGram = checked((int)totalWeightGram),
                        LengthCm = lengthCm,
                        WidthCm = widthCm,
                        HeightCm = heightCm
                    }
                    : null,
                Items = hasDimensions && isHeavyParcel ? new[]
                    {
                        BuildHeavyItemFromProduct(
                            product,
                            weightGram,
                            lengthCm,
                            widthCm,
                            heightCm,
                            quantity)
                    }
                    : Array.Empty<GhnItemSnapshotDto>()
                    };

            return Result<GhnParcelInfoResponse>.Success(response);
        }

        public async Task<Result<GhnShippingPreviewResponse>> PreviewGhnShippingAsync(Guid negotiationId, Guid currentUserId, GhnShippingPreviewRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var negotiationResult = await GetAuthorizedNegotiationAsync(negotiationId, currentUserId, cancellationToken);
            if (!negotiationResult.IsSuccess)
                return Result<GhnShippingPreviewResponse>.Fail(negotiationResult.Error!);

            var contextError = await ValidateAgreementGhnContextAsync(negotiationId, request.AgreementType, request.DeliveryMethod, cancellationToken);
            if (contextError != null) return Result<GhnShippingPreviewResponse>.Fail(contextError);

            var validationResult = await _shippingPreviewValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage).Distinct());
                return Result<GhnShippingPreviewResponse>.Fail(new Error("ShippingFee.InvalidRequest", errorMessage));
            }

            var product = await _productRepo.GetDetailByPostIdAsync(negotiationResult.Data!.PostId, cancellationToken);

            var resolvedResult = await ResolveParcelAsync(request, product, negotiationResult.Data!, cancellationToken);
            if (!resolvedResult.IsSuccess)
                return Result<GhnShippingPreviewResponse>.Fail(resolvedResult.Error!);

            var resolved = resolvedResult.Data!;
            var resolvedValidation = await _shippingPreviewValidator.ValidateAsync(resolved, cancellationToken);
            if (!resolvedValidation.IsValid)
                return Result<GhnShippingPreviewResponse>.Fail(new Error("ShippingFee.InvalidRequest",
                    string.Join(", ", resolvedValidation.Errors.Select(x => x.ErrorMessage).Distinct())));

            GhnPreviewQuote quote;
            try
            {
                var services = await _ghnService.GetAvailableServicesAsync(
                    resolved.Sender!.Address.DistrictId, resolved.Receiver!.Address.DistrictId, cancellationToken);
                if (!services.Any(x => x.ServiceTypeId == resolved.ServiceTypeId))
                    return Result<GhnShippingPreviewResponse>.Fail(new Error("Ghn.ServiceUnavailable",
                        "GHN không có dịch vụ phù hợp với kiện hàng trên tuyến này."));
                quote = await _ghnService.PreviewOrderAsync(resolved, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                return Result<GhnShippingPreviewResponse>.Fail(new Error("ShippingFee.InvalidRequest", exception.Message));
            }
            catch (Exception exception) when (exception is IGhnApiError { CodeMessage: "SHOP_NOT_FOUND" })
            {
                return Result<GhnShippingPreviewResponse>.Fail(new Error("Ghn.ShopNotFound", "Không tìm thấy shop GHN được cấu hình."));
            }
            catch (Exception exception) when (exception is IGhnApiError
                { CodeMessage: "ROUTE_NOT_FOUND_SERVICE" or "SERVICE_NOT_FOUND_CONFIG_FEE" })
            {
                return Result<GhnShippingPreviewResponse>.Fail(new Error("Ghn.ServiceUnavailable",
                    "Dịch vụ GHN chưa hỗ trợ tuyến hoặc chưa có cấu hình phí cho tuyến này."));
            }
            catch (Exception exception) when (exception is IGhnApiError
                { CodeMessage: "USER_ERR_COMMON" or "WARD_IS_INVALID" or "PHONE_INVALID" })
            {
                return Result<GhnShippingPreviewResponse>.Fail(new Error("ShippingFee.InvalidRequest", exception.Message));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Xem trước thông tin vận chuyển GHN thất bại cho NegotiationId {NegotiationId}.", negotiationId);
                return Result<GhnShippingPreviewResponse>.Fail(new Error(
                    "Ghn.PreviewFailed",
                    "Không thể xem trước thông tin vận chuyển GHN ở thời điểm hiện tại."));
            }

            var response = new GhnShippingPreviewResponse
            {
                NegotiationId = negotiationId,
                ServiceTypeId = resolved.ServiceTypeId,
                TotalFee = quote.TotalFee,
                ExpectedDeliveryAt = quote.ExpectedDeliveryAt,
                WeightGram = resolved.WeightGram!.Value,
                LengthCm = resolved.LengthCm!.Value,
                WidthCm = resolved.WidthCm!.Value,
                HeightCm = resolved.HeightCm!.Value,
                ParcelCount = resolved.ParcelCount,
                LightParcel = resolved.ServiceTypeId == 2
                    ? new GhnLightParcelSnapshotDto
                    {
                        WeightGram = resolved.WeightGram!.Value,
                        LengthCm = resolved.LengthCm!.Value,
                        WidthCm = resolved.WidthCm!.Value,
                        HeightCm = resolved.HeightCm!.Value
                    }
                    : null,
                Items = resolved.Items.Select(item => new GhnItemSnapshotDto
                    {
                        Name = item.Name,
                        Code = item.Code,
                        Quantity = item.Quantity,
                        WeightGram = item.WeightGram,
                        LengthCm = item.LengthCm,
                        WidthCm = item.WidthCm,
                        HeightCm = item.HeightCm
                    }).ToList()
            };

            var info = new GhnShippingInfo
            {
                Sender = resolved.Sender, Receiver = resolved.Receiver,
                ServiceTypeId = resolved.ServiceTypeId, ParcelCount = resolved.ParcelCount,
                WeightGram = resolved.WeightGram, LengthCm = resolved.LengthCm,
                WidthCm = resolved.WidthCm, HeightCm = resolved.HeightCm,
                RequiredNote = resolved.RequiredNote, Content = resolved.Content, Items = response.Items
            };
            var quotedAt = DateTimeOffset.UtcNow;
            var confirmedQuote = new GhnQuoteSnapshotDto
            {
                TotalFee = quote.TotalFee, Breakdown = new GhnFeeBreakdownSnapshotDto(),
                QuotedAt = quotedAt, InputHash = GhnShippingCalculationHelper.SnapshotHash(info),
                ExpectedDeliveryAt = quote.ExpectedDeliveryAt ?? default
            };
            response.PreviewToken = GhnShippingCalculationHelper.IssuePreviewToken($"agreement:{negotiationId}", info,
                confirmedQuote, _configuration["Jwt:SecretKey"]!);
            response.ExpiresAt = quotedAt.AddMinutes(30);
            info.PreviewToken = response.PreviewToken;
            response.ShippingInfo = info;
            return Result<GhnShippingPreviewResponse>.Success(response);
        }

        public async Task<Result<GhnLeadtimeResponse>> GetGhnLeadtimeAsync(Guid negotiationId, Guid userId,
            GhnLeadtimeRequest request, CancellationToken cancellationToken = default)
        {
            var authorization = await GetAuthorizedNegotiationAsync(negotiationId, userId, cancellationToken);
            if (!authorization.IsSuccess) return Result<GhnLeadtimeResponse>.Fail(authorization.Error!);
            var contextError = await ValidateAgreementGhnContextAsync(negotiationId, request.AgreementType, request.DeliveryMethod, cancellationToken);
            if (contextError != null) return Result<GhnLeadtimeResponse>.Fail(contextError);
            try { return Result<GhnLeadtimeResponse>.Success(await _ghnService.GetLeadtimeAsync(request, cancellationToken)); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (ArgumentException ex) { return Result<GhnLeadtimeResponse>.Fail(new Error("Ghn.InvalidLeadtimeRequest", ex.Message)); }
            catch (Exception ex) when (ex is IGhnApiError { HttpStatusCode: 400 })
            { return Result<GhnLeadtimeResponse>.Fail(new Error("Ghn.InvalidLeadtimeRequest", ex.Message)); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GHN leadtime failed for {NegotiationId}", negotiationId);
                return Result<GhnLeadtimeResponse>.Fail(new Error("Ghn.LeadtimeFailed", "Chưa lấy được thời gian dự kiến từ GHN."));
            }
        }
        private async Task<Error?> ValidateAgreementGhnContextAsync(Guid negotiationId, AgreementType? agreementType,
            DeliveryMethod? deliveryMethod, CancellationToken ct)
        {
            if (!GhnShippingCalculationHelper.IsAgreementGhnDelivery(agreementType, deliveryMethod))
                return new Error("Ghn.InvalidDeliveryContext", "Chỉ tính GHN khi chọn No_Inspection và GhnDelivery.");
            var agreement = await _agreementRepo.GetByNegotiationIdAsync(negotiationId, ct);
            if (agreement == null) return null; // FE previews the draft before saving an agreement.
            if (agreement.AgreementStatus is (int)AgreementStatus.Cancelled or (int)AgreementStatus.Expired)
                return new Error("Ghn.InvalidDeliveryContext", "Thỏa thuận đã bị hủy hoặc hết hạn.");
            if (await _orderRepo.GetByAgreementIdAsync(agreement.AgreementId, ct) != null)
                return new Error("Ghn.OrderAlreadyExists", "Đã có đơn hàng; không thể tính lại GHN ở bước agreement.");
            if (agreement.AgreementType == (int)AgreementType.Inspection)
                return new Error("Ghn.InvalidDeliveryContext", "Đơn kiểm định chỉ chọn GHN khi thu gom sau kiểm định.");
            return null;
        }

        private async Task<GhnContactSnapshotDto> GetContactDefaultsAsync(Guid userId, CancellationToken ct)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId, ct);
            var user = await _userRepo.GetByIdAsync(userId, ct);
            var fullName = profile?.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = (await _businessProfileRepo.GetByUserIdAsync(userId, ct))?.FullName;
            return new GhnContactSnapshotDto
            {
                FullName = fullName?.Trim() ?? string.Empty,
                Phone = user?.PhoneNumber?.Trim() ?? string.Empty,
                Address = new GhnAddressSnapshotDto
                {
                    ProvinceName = string.Empty, DistrictName = string.Empty,
                    WardCode = string.Empty, WardName = string.Empty, AddressDetail = string.Empty
                }
            };
        }

        private async Task<Result<negotiation>> GetAuthorizedNegotiationAsync(Guid negotiationId, Guid currentUserId, CancellationToken cancellationToken)
        {
            var negotiation = await _negotiationRepo.GetByIdAsync(negotiationId, cancellationToken);
            if (negotiation is null)
                return Result<negotiation>.Fail(new Error("Negotiation.NotFound", "Không tìm thấy cuộc thương lượng."));

            bool isSeller = negotiation.SellerId == currentUserId;
            bool isBuyer = negotiation.BuyerId == currentUserId;

            if (!isSeller && !isBuyer)
                return Result<negotiation>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền truy cập."));

            if (negotiation.NegotiationStatus is NegotiationStatus.Cancelled or NegotiationStatus.Closed or NegotiationStatus.Expired)
                return Result<negotiation>.Fail(new Error("Negotiation.Cancelled", "Không thể xem trước cho cuộc thương lượng đã bị hủy."));

            var offer = await _offerRepo.GetByIdAsync(negotiation.OfferId, cancellationToken);
            if (offer?.OfferStatus == OfferStatus.Rejected)
                return Result<negotiation>.Fail(new Error("Ghn.InvalidDeliveryContext", "Offer đã bị từ chối."));

            return Result<negotiation>.Success(negotiation);
        }

        private async Task<int> GetOfferQuantityAsync(negotiation negotiation, CancellationToken cancellationToken)
        {
            if (negotiation.FinalQuantity is > 0)
                return negotiation.FinalQuantity.Value;

            var offer = await _offerRepo.GetByIdAsync(negotiation.OfferId, cancellationToken);
            return offer?.OfferQuantity ?? 1;
        }

        private async Task<Result<GhnShippingPreviewRequest>> ResolveParcelAsync(
            GhnShippingPreviewRequest request, product? product, negotiation negotiation,
            CancellationToken cancellationToken)
        {
            var quantity = await GetOfferQuantityAsync(negotiation, cancellationToken);
            if (quantity <= 0)
                return Result<GhnShippingPreviewRequest>.Fail(new Error("ShippingFee.InvalidRequest", "Số lượng sản phẩm phải lớn hơn 0."));

            int productWeight = 0, productLength = 0, productWidth = 0, productHeight = 0;
            var hasProductDimensions = product != null && TryNormalizeProductMeasurements(
                product, out productWeight, out productLength, out productWidth, out productHeight);
            var items = request.Items;
            try
            {
                if (request.ServiceTypeId == 5 && items.Count == 0)
                {
                    if (!hasProductDimensions)
                        return Result<GhnShippingPreviewRequest>.Fail(new Error("Ghn.ParcelInformationRequired", "Cần thông số sản phẩm hoặc danh sách kiện hàng."));
                    items = new[] { new CalculateGhnFeeItemRequest
                    {
                        Name = string.IsNullOrWhiteSpace(product!.ProductName) ? "Sản phẩm HomeCycle" : product.ProductName.Trim(),
                        Quantity = quantity,
                        WeightGram = productWeight,
                        LengthCm = productLength,
                        WidthCm = productWidth,
                        HeightCm = productHeight
                    }};
                }

                long? itemWeight = items.Count > 0 && items.All(x => x.WeightGram > 0)
                    ? items.Aggregate(0L, (sum, x) => checked(sum + (long)x.WeightGram * x.Quantity))
                    : null;
                if (request.ServiceTypeId == 5 && request.WeightGram.HasValue && request.WeightGram.Value != itemWeight)
                    return Result<GhnShippingPreviewRequest>.Fail(new Error("ShippingFee.InvalidRequest", "Tổng khối lượng phải bằng tổng weight × quantity của các kiện."));

                long? weight = request.WeightGram ?? itemWeight ??
                    (hasProductDimensions ? checked((long)productWeight * quantity) : (long?)null);
                // Chỉ suy kích thước từ một đơn vị; nhiều sản phẩm/kiện cần kích thước đóng gói thực tế.
                var singleItem = items.Count == 1 && items[0].Quantity == 1 ? items[0] : null;
                int? length = request.LengthCm ?? (singleItem?.LengthCm > 0 ? singleItem.LengthCm :
                    hasProductDimensions && quantity == 1 && items.Count == 0 ? productLength : (int?)null);
                int? width = request.WidthCm ?? (singleItem?.WidthCm > 0 ? singleItem.WidthCm :
                    hasProductDimensions && quantity == 1 && items.Count == 0 ? productWidth : (int?)null);
                int? height = request.HeightCm ?? (singleItem?.HeightCm > 0 ? singleItem.HeightCm :
                    hasProductDimensions && quantity == 1 && items.Count == 0 ? productHeight : (int?)null);
                if (weight is null || length is null || width is null || height is null)
                    return Result<GhnShippingPreviewRequest>.Fail(new Error("Ghn.ParcelInformationRequired", "Cần đủ tổng khối lượng và kích thước đóng gói cấp đơn; nhiều sản phẩm/kiện cần nhập kích thước thực tế."));
                if (weight is < 1 or > 50_000)
                    return Result<GhnShippingPreviewRequest>.Fail(new Error("ShippingFee.InvalidRequest", "Khối lượng preview phải từ 1 đến 50.000 gram."));
                var expectedType = weight >= HeavyParcelThresholdGram || request.ParcelCount > 1 ? 5 : 2;
                if (request.ServiceTypeId != expectedType)
                    return Result<GhnShippingPreviewRequest>.Fail(new Error("ShippingFee.InvalidRequest", $"Thông tin kiện yêu cầu ServiceTypeId = {expectedType}."));

                var content = string.IsNullOrWhiteSpace(request.Content)
                    ? (string.IsNullOrWhiteSpace(product?.ProductName) ? "Sản phẩm HomeCycle" : product.ProductName.Trim())
                    : request.Content.Trim();
                return Result<GhnShippingPreviewRequest>.Success(new GhnShippingPreviewRequest
                {
                    Sender = request.Sender,
                    Receiver = request.Receiver,
                    ServiceTypeId = request.ServiceTypeId,
                    RequiredNote = request.RequiredNote,
                    Content = content,
                    ParcelCount = request.ParcelCount,
                    WeightGram = checked((int)weight.Value),
                    LengthCm = length,
                    WidthCm = width,
                    HeightCm = height,
                    Items = items
                });
            }
            catch (OverflowException)
            {
                return Result<GhnShippingPreviewRequest>.Fail(new Error("ShippingFee.InvalidRequest", "Tổng khối lượng vượt phạm vi cho phép."));
            }
        }
        private static bool TryNormalizeProductMeasurements(product product, out int weightGram, out int lengthCm, out int widthCm, out int heightCm)
        {
            weightGram = 0;
            lengthCm = 0;
            widthCm = 0;
            heightCm = 0;

            if (product.Weight is null or <= 0 ||
                product.Length is null or <= 0 ||
                product.Width is null or <= 0 ||
                product.Height is null or <= 0)
            {
                return false;
            }

            try
            {
                // Làm tròn lên để không khai thiếu khối lượng/kích thước.
                weightGram = checked((int)Math.Ceiling(product.Weight.Value * 1000));

                var sides = new[]
                {
                    checked((int)Math.Ceiling(product.Length.Value)),
                    checked((int)Math.Ceiling(product.Width.Value)),
                    checked((int)Math.Ceiling(product.Height.Value))
                }
                .OrderByDescending(x => x)
                .ToArray();

                lengthCm = sides[0];
                widthCm = sides[1];
                heightCm = sides[2];

                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static GhnItemSnapshotDto BuildHeavyItemFromProduct(product product, int weightGram, int lengthCm, int widthCm, int heightCm, int quantity)
        {
            return new GhnItemSnapshotDto
            {
                Name = string.IsNullOrWhiteSpace(product.ProductName)
                    ? "Sản phẩm HomeCycle"
                    : product.ProductName.Trim(),
                Quantity = quantity,
                WeightGram = weightGram,
                LengthCm = lengthCm,
                WidthCm = widthCm,
                HeightCm = heightCm
            };
        }

        private async Task<Result<bool>> ComputeShippingFeeAsync(AgreementDetailsDto details, Guid postId, Guid negotiationId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(details);

            details.EstimatedShippingFee = null;

            if (details.DeliveryMethod != DeliveryMethod.GhnDelivery)
                return Result<bool>.Success(true);

            var ghn = details.GhnInfo;

            if (ghn is null)
            {
                return Result<bool>.Fail(
                    new Error(
                        "Ghn.ShippingInfoRequired",
                        "Chưa có thông tin vận chuyển GHN."));
            }

            GhnQuoteSnapshotDto confirmedQuote;
            try { confirmedQuote = GhnShippingCalculationHelper.ConfirmPreview($"agreement:{negotiationId}", ghn, _configuration["Jwt:SecretKey"]!); }
            catch (ArgumentException ex) { return Result<bool>.Fail(new Error("Ghn.InvalidPreview", ex.Message)); }

            var senderAddress = ghn.Sender?.Address;
            var receiverAddress = ghn.Receiver?.Address;

            if (senderAddress is null ||
                receiverAddress is null ||
                senderAddress.DistrictId <= 0 ||
                string.IsNullOrWhiteSpace(senderAddress.WardCode) ||
                receiverAddress.DistrictId <= 0 ||
                string.IsNullOrWhiteSpace(receiverAddress.WardCode))
            {
                return Result<bool>.Fail(
                    new Error(
                        "Ghn.AddressRequired",
                        "Cần đầy đủ quận/huyện và phường/xã của người gửi và người nhận."));
            }

            if (ghn.ServiceTypeId is not (2 or 5))
            {
                return Result<bool>.Fail(
                    new Error(
                        "Ghn.InvalidServiceType",
                        "Loại dịch vụ GHN chỉ nhận 2 (hàng nhẹ) hoặc 5 (hàng nặng)."));
            }

            var feeRequest = HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.BuildFeeRequest(ghn, null);
            if (!feeRequest.IsSuccess) return Result<bool>.Fail(feeRequest.Error!);
            var request = feeRequest.Data!;
            var validationResult = await _shippingFeeValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var message = string.Join(", ", validationResult.Errors
                        .Select(error => error.ErrorMessage)
                        .Distinct());

                return Result<bool>.Fail(new Error("Ghn.InvalidFeeRequest", message));
            }

            try
            {
                var quote = await _ghnService.GetShippingFeeAsync(request, cancellationToken);
                if (quote.TotalFee != confirmedQuote.TotalFee)
                    return Result<bool>.Fail(new Error("Ghn.QuoteChanged", "Phí GHN đã thay đổi; vui lòng preview và xác nhận lại."));
                ghn.Quote = confirmedQuote;
                details.EstimatedShippingFee = quote.TotalFee;

                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Không biến request bị hủy thành lỗi GHN.
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Tính phí vận chuyển GHN thất bại cho PostId {PostId}.",
                    postId);

                return Result<bool>.Fail(
                    new Error(
                        "Ghn.CalculateFeeFailed",
                        "Không thể tính phí vận chuyển GHN ở thời điểm hiện tại."));
            }
        }

        private async Task<Result<AgreementProductSnapshot>> BuildProductSnapshotAsync(Guid postId, CancellationToken cancellationToken)
        {
            var post = await _postRepo.GetByIdAsync(postId, cancellationToken);
            if (post is null)
                return Result<AgreementProductSnapshot>.Fail(
                    new Error("Post.NotFound", "Không tìm thấy bài đăng."));

            var product = await _productRepo.GetDetailByPostIdAsync(postId, cancellationToken);
            if (product is null)
                return Result<AgreementProductSnapshot>.Fail(
                    new Error("Product.NotFound", "Không tìm thấy sản phẩm liên kết với bài đăng này."));

            var mediaResult = await _mediaService.GetByTargetsAsync(
                targetIds: new[] { postId },
                targetType: "Post",
                cancellationToken);

            var mediaList = Array.Empty<PostMediaSnapshotInfo>();

            // Kiểm tra kết quả trả về từ MediaService và danh sách theo postId
            if (mediaResult.IsSuccess && mediaResult.Data != null && mediaResult.Data.TryGetValue(postId, out var responseMedias))
            {
                mediaList = responseMedias
                    .Select(m => new PostMediaSnapshotInfo
                    {
                        MediaId = m.MediaId,
                        Url = m.Url,
                        FileName = m.FileName,
                        FileSize = m.FileSize,
                        DisplayOrder = m.DisplayOrder
                    })
                    .ToArray();
            }

            var snapshot = new AgreementProductSnapshot
            {
                PostInfo = new PostSnapshotInfo
                {
                    PostId = post.PostId,
                    OwnerId = post.OwnerId,
                    Description = post.Description,
                    BasePrice = post.BasePrice,
                    PostType = post.PostType,
                    PostedQuantity = post.Quantity,
                    CreatedAt = post.CreatedAt
                },

                ProductInfo = new ProductSnapshotInfo
                {
                    ProductId = product.ProductId,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category?.CategoryName,
                    ProductTypeId = product.ProductTypeId,
                    ProductTypeName = product.ProductType?.ProductTypeName,
                    BrandId = product.BrandId,
                    BrandName = product.Brand?.BrandName,
                    ProductName = product.ProductName,
                    ModelNumber = product.ModelNumber,
                    OriginalPrice = product.OriginalPrice,
                    SpaceUsage = product.SpaceUsage,
                    FunctionalityStatus = product.FunctionalityStatus,
                    DamageLevel = product.DamageLevel,
                    UsageDuration = product.UsageDuration,

                    Measurements = new ProductMeasurementSnapshotInfo
                    {
                        // Map trực tiếp từ các trường Measurements nằm ở gốc thực thể Product
                        Weight = product.Weight,
                        Length = product.Length,
                        Width = product.Width,
                        Height = product.Height
                    }
                },

                // Gán danh sách Media đã chuẩn hóa
                Medias = mediaList
            };

            return Result<AgreementProductSnapshot>.Success(snapshot);
        }

    }
}
