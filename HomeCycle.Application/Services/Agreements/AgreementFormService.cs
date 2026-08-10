using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Agreements;
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
        private readonly IChatRealtimePublisher _chatRealtimePublisher;
        private readonly IMediaService _mediaService;
        private readonly IGhnService _ghnService;
        private readonly IMapper _mapper;
        private readonly ILogger<AgreementFormService> _logger;
        private readonly IPostRepository _postRepo;
        private readonly IOfferRepository _offerRepo;
        private readonly IProductRepository _productRepo;
        private readonly IValidator<CreateAgreementFormRequest> _createValidator;
        private readonly IValidator<UpdateAgreementFormRequest> _updateValidator;
        private readonly IValidator<CalculateGhnFeeRequest> _shippingFeeValidator;

        public AgreementFormService(
            IUnitOfWork unitOfWork,
            IAgreementFormRepository agreementRepo,
            INegotiationRepository negotiationRepo,
            IMessageRepository messageRepo,
            IChatRealtimePublisher chatRealtimePublisher,
            IMediaService mediaService,
            IGhnService ghnService,
            IMapper mapper,
            ILogger<AgreementFormService> logger,
            IPostRepository postRepo,
            IOfferRepository offerRepo,
            IProductRepository productRepo,
            IValidator<CreateAgreementFormRequest> createValidator,
            IValidator<UpdateAgreementFormRequest> updateValidator,
            IValidator<CalculateGhnFeeRequest> shippingFeeValidator)
        {
            _unitOfWork = unitOfWork;
            _agreementRepo = agreementRepo;
            _negotiationRepo = negotiationRepo;
            _messageRepo = messageRepo;
            _chatRealtimePublisher = chatRealtimePublisher;
            _mediaService = mediaService;
            _ghnService = ghnService;
            _mapper = mapper;
            _logger = logger;
            _postRepo = postRepo;
            _offerRepo = offerRepo;
            _productRepo = productRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _shippingFeeValidator = shippingFeeValidator;
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
            if (request.AgreementDetails?.DeliveryMethod == DeliveryMethod.GhnDelivery)
            {
                var negotiation = await _negotiationRepo.GetByIdAsync(request.NegotiationId, cancellationToken);
                if (negotiation == null)
                    return Result<Guid>.Fail(new Error("Negotiation.NotFound", "Không tìm thấy cuộc thương lượng."));

                var feeResult = await ComputeShippingFeeAsync(request.AgreementDetails, negotiation.PostId, cancellationToken);
                if (!feeResult.IsSuccess)
                    return Result<Guid>.Fail(feeResult.Error!);
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var negotiation = await _negotiationRepo.GetByIdAsync(request.NegotiationId, cancellationToken);
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

                var snapshotResult = await BuildProductSnapshotAsync(post.PostId, cancellationToken);
                if (!snapshotResult.IsSuccess)
                    return Result<Guid>.Fail(snapshotResult.Error!);

                var newAgreement = new agreement_form
                {
                    AgreementId = Guid.NewGuid(),
                    NegotiationId = request.NegotiationId,
                    PostId = negotiation.PostId,
                    SellerId = negotiation.SellerId,
                    BuyerId = negotiation.BuyerId,


                    InitialPrice = offer.OfferPrice,
                    FinalPrice = negotiation.FinalPrice,
                    Quantity = offer.OfferQuantity,

                    AgreementType = (int)request.AgreementType,
                    PaymentType = (int)request.PaymentType,
                    AgreementStatus = (int)AgreementStatus.Pending,

                    PSnapshot = JsonSerializer.Serialize(snapshotResult.Data),
                    AgreementDetailsJsonb = JsonSerializer.Serialize(request.AgreementDetails),

                    CreatedAt = DateTime.UtcNow,
                    BuyerConfirmedAt = null,
                    SellerConfirmedAt = DateTime.UtcNow
                };

                //negotiation.NegotiationStatus = 1;

                await _agreementRepo.AddAsync(newAgreement, cancellationToken);
                await _negotiationRepo.UpdateAsync(negotiation, cancellationToken);

                var agreementMessage = new message
                {
                    MessageId = Guid.NewGuid(),
                    NegotiationId = request.NegotiationId,
                    SenderId = negotiation.SellerId,        // người tạo = seller -> hiện bên phải
                    MessageType = MessageType.Agreement,
                    MessageContent = "Đã tạo thỏa thuận mua bán, vui lòng kiểm tra và xác nhận.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _messageRepo.AddAsync(agreementMessage, cancellationToken);


                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = _mapper.Map<MessageResponse>(agreementMessage);
                await PublishMessageCreatedSafelyAsync(request.NegotiationId, response);

                return Result<Guid>.Success(newAgreement.AgreementId);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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

            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            bool isSeller = agreement.SellerId == currentUserId;
            bool isBuyer = agreement.BuyerId == currentUserId;
            if (!isSeller && !isBuyer)
                return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền cập nhật thỏa thuận này."));

            if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                return Result<AgreementActionResponse>.Fail(new Error(
                    "Agreement.InvalidStatus",
                    "Thỏa thuận đã được cả hai bên chốt. Vui lòng yêu cầu mở lại (Request Edit) trước khi chỉnh sửa."));

            // Giao hàng qua GHN: server tự gọi lại API tính phí để con số trong hợp đồng luôn chính xác
            if (request.AgreementDetails?.DeliveryMethod == DeliveryMethod.GhnDelivery)
            {
                var feeResult = await ComputeShippingFeeAsync(request.AgreementDetails, agreement.PostId, cancellationToken);
                if (!feeResult.IsSuccess)
                    return Result<AgreementActionResponse>.Fail(feeResult.Error!);
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

            await _agreementRepo.UpdateAsync(agreement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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


        public async Task<Result<AgreementActionResponse>> AcceptAgreementAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            // 1. Lấy dữ liệu
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            // 2. Phân quyền: cả Seller và Buyer đều có thể tự accept phần của mình
            bool isSeller = agreement.SellerId == currentUserId;
            bool isBuyer = agreement.BuyerId == currentUserId;
            if (!isSeller && !isBuyer)
                return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền chấp nhận thỏa thuận này."));

            // 3. State Machine: Chỉ được Accept khi đang Pending
            if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.InvalidStatus", "Thỏa thuận không ở trạng thái chờ xác nhận."));

            // 4. Không cho accept lặp lại nếu bên đó đã confirm rồi (idempotent - tránh nhầm lẫn ở FE)
            if (isSeller && agreement.SellerConfirmedAt != null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.AlreadyConfirmed", "Bạn đã xác nhận thỏa thuận này rồi."));
            if (isBuyer && agreement.BuyerConfirmedAt != null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.AlreadyConfirmed", "Bạn đã xác nhận thỏa thuận này rồi."));

            // 5. Set confirm cho đúng phần của người gọi (không đụng vào phần bên kia)
            var now = DateTime.UtcNow;
            if (isSeller)
                agreement.SellerConfirmedAt = now;
            else
                agreement.BuyerConfirmedAt = now;

            // 6. Nếu cả 2 đã confirm -> tự động chuyển sang chờ thanh toán
            bool bothConfirmed = agreement.SellerConfirmedAt != null && agreement.BuyerConfirmedAt != null;
            if (bothConfirmed)
                agreement.AgreementStatus = (int)AgreementStatus.Awaiting_Payment;

            // 7. Cập nhật DB (Không cần BeginTransaction vì chỉ tác động 1 bảng)
            await _agreementRepo.UpdateAsync(agreement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        public async Task<Result<AgreementActionResponse>> RequestEditAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            // 1. Lấy dữ liệu
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<AgreementActionResponse>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            // 2. Phân quyền: cả Seller và Buyer đều có quyền yêu cầu mở lại để chỉnh sửa
            bool isSeller = agreement.SellerId == currentUserId;
            bool isBuyer = agreement.BuyerId == currentUserId;
            if (!isSeller && !isBuyer)
                return Result<AgreementActionResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền yêu cầu chỉnh sửa thỏa thuận này."));

            // 3. State Machine: chỉ dùng để MỞ LẠI khi đã Awaiting_Payment (cả 2 đã chốt).
            //    Nếu còn Pending thì gọi thẳng Update là đủ, không cần qua bước này.
            if (agreement.AgreementStatus != (int)AgreementStatus.Awaiting_Payment)
                return Result<AgreementActionResponse>.Fail(new Error(
                    "Agreement.InvalidStatus",
                    "Chỉ có thể yêu cầu mở lại khi thỏa thuận đã được cả hai bên chốt và đang chờ thanh toán."));

            // 4. Mở lại: đưa về Pending, reset cả 2 confirm (nội dung sắp bị sửa nên cả 2 phải xác nhận lại)
            agreement.AgreementStatus = (int)AgreementStatus.Pending;
            agreement.SellerConfirmedAt = null;
            agreement.BuyerConfirmedAt = null;

            await _agreementRepo.UpdateAsync(agreement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AgreementActionResponse>.Success(new AgreementActionResponse
            {
                Message = "Đã mở lại thỏa thuận để chỉnh sửa. Cả hai bên cần xác nhận lại sau khi cập nhật.",
                AgreementId = agreement.AgreementId,
                AgreementStatus = (AgreementStatus)agreement.AgreementStatus,
                SellerConfirmed = false,
                BuyerConfirmed = false,
                SellerConfirmedAt = null,
                BuyerConfirmedAt = null
            });
        }

        public async Task<Result<PagedResult<agreement_form>>> GetPendingPaymentAsync(
            Guid buyerId, PendingAgreementSearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _agreementRepo.GetPendingPaymentByBuyerAsync(buyerId, request, cancellationToken);
            return Result<PagedResult<agreement_form>>.Success(result);
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

        private async Task<Result<bool>> ComputeShippingFeeAsync(AgreementDetailsDto details, Guid postId, CancellationToken cancellationToken)
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

            CalculateGhnFeeRequest request;

            if (ghn.ServiceTypeId == 2)
            {
                // Hàng nhẹ: lấy kích thước và khối lượng cấp đơn từ Product.
                var product = await _productRepo.GetDetailByPostIdAsync(
                    postId,
                    cancellationToken);

                if (product is null)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Product.NotFound",
                            "Không tìm thấy sản phẩm của bài đăng."));
                }

                if (product.Weight is null or <= 0 ||
                    product.Length is null or <= 0 ||
                    product.Width is null or <= 0 ||
                    product.Height is null or <= 0)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Ghn.ParcelInformationRequired",
                            "Sản phẩm chưa có đầy đủ khối lượng và kích thước để tính phí GHN."));
                }

                int weightGram;
                int lengthCm;
                int widthCm;
                int heightCm;

                try
                {
                    // Làm tròn lên để không khai thiếu khối lượng/kích thước.
                    weightGram = checked(
                        (int)Math.Ceiling(product.Weight.Value * 1000));

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
                }
                catch (OverflowException)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Ghn.ParcelInformationInvalid",
                            "Khối lượng hoặc kích thước sản phẩm vượt phạm vi cho phép."));
                }

                request = new CalculateGhnFeeRequest
                {
                    FromDistrictId = senderAddress.DistrictId,
                    FromWardCode = senderAddress.WardCode.Trim(),

                    ToDistrictId = receiverAddress.DistrictId,
                    ToWardCode = receiverAddress.WardCode.Trim(),

                    ServiceTypeId = 2,
                    WeightGram = weightGram,
                    LengthCm = lengthCm,
                    WidthCm = widthCm,
                    HeightCm = heightCm,

                    // Hàng nhẹ không gửi Items
                    Items = Array.Empty<CalculateGhnFeeItemRequest>()
                };
            }
            else
            {
                // Hàng nặng: mỗi phần tử là một kiện hàng
                var items = ghn.Items?
                    .Select(item => new CalculateGhnFeeItemRequest
                    {
                        Name = item.Name?.Trim() ?? string.Empty,
                        Quantity = item.Quantity,
                        WeightGram = item.WeightGram,
                        LengthCm = item.LengthCm,
                        WidthCm = item.WidthCm,
                        HeightCm = item.HeightCm
                    })
                    .ToList()
                    ?? new List<CalculateGhnFeeItemRequest>();

                if (items.Count == 0)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Ghn.HeavyItemsRequired",
                            "Hàng nặng phải có ít nhất một kiện hàng."));
                }

                long totalWeight;

                try
                {
                    totalWeight = items.Aggregate(0L, (total, item) => checked(total + (long)item.WeightGram * item.Quantity));
                }
                catch (OverflowException)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Ghn.TotalWeightInvalid",
                            "Tổng khối lượng kiện hàng không hợp lệ."));
                }

                if (totalWeight is < 1 or > 1_600_000)
                {
                    return Result<bool>.Fail(
                        new Error(
                            "Ghn.TotalWeightInvalid",
                            "Tổng khối lượng kiện hàng phải từ 1 đến 1.600.000 gram."));
                }

                request = new CalculateGhnFeeRequest
                {
                    FromDistrictId = senderAddress.DistrictId,
                    FromWardCode = senderAddress.WardCode.Trim(),

                    ToDistrictId = receiverAddress.DistrictId,
                    ToWardCode = receiverAddress.WardCode.Trim(),

                    ServiceTypeId = 5,
                    WeightGram = checked((int)totalWeight),

                    // Hàng nặng lấy kích thước từ Items.
                    LengthCm = null,
                    WidthCm = null,
                    HeightCm = null,
                    Items = items
                };
            }

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
