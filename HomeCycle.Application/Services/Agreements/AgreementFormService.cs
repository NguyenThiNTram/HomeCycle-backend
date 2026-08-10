using AutoMapper;
using FluentValidation;
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
            IValidator<UpdateAgreementFormRequest> updateValidator)
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

                response.CanEdit = isSeller && isPending;
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
                AgreementDetails = string.IsNullOrEmpty(agreement.AgreementDetailsJsonb)
                    ? null
                    : JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb)
            };

            return Result<AgreementDetailResponse>.Success(response);
        }

        public async Task<Result<bool>> UpdateAgreementAsync(Guid agreementId, UpdateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<bool>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<bool>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.SellerId != currentUserId)
                return Result<bool>.Fail(new Error("Auth.Forbidden", "Chỉ người bán mới được cập nhật thỏa thuận."));

            if (agreement.BuyerConfirmedAt != null)
                return Result<bool>.Fail(new Error("Agreement.Locked", "Không thể sửa đổi vì người mua đã xác nhận. Vui lòng hủy thỏa thuận nếu muốn thay đổi."));

            if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                return Result<bool>.Fail(new Error("Agreement.InvalidStatus", "Chỉ có thể sửa khi thỏa thuận đang chờ xác nhận."));

            // Nếu Seller sửa thì reset lại Confirmed của cả 2 bên (nếu trước đó đã có người lỡ ấn Confirm)
            agreement.AgreementType = (int)request.AgreementType;
            agreement.PaymentType = (int)request.PaymentType;
            agreement.AgreementDetailsJsonb = JsonSerializer.Serialize(request.AgreementDetails);
            agreement.SellerConfirmedAt = DateTime.UtcNow;
            agreement.BuyerConfirmedAt = null;

            await _agreementRepo.UpdateAsync(agreement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }


        public async Task<Result<bool>> AcceptAgreementAsync(Guid agreementId, Guid buyerId, CancellationToken cancellationToken = default)
        {
            // 1. Lấy dữ liệu
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<bool>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            // 2. Phân quyền: Chỉ Buyer mới có nút Accept
            if (agreement.BuyerId != buyerId)
                return Result<bool>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền chấp nhận thỏa thuận này."));

            // 3. State Machine: Chỉ được Accept khi đang Pending
            if (agreement.AgreementStatus != (int)AgreementStatus.Pending)
                return Result<bool>.Fail(new Error("Agreement.InvalidStatus", "Thỏa thuận không ở trạng thái chờ xác nhận."));

            // 4. Defensive Check: Đảm bảo Seller đã chốt rồi
            if (agreement.SellerConfirmedAt == null)
                return Result<bool>.Fail(new Error("Agreement.NotReady", "Người bán chưa chốt thỏa thuận, không thể chấp nhận."));

            // 5. Thay đổi trạng thái
            agreement.BuyerConfirmedAt = DateTime.UtcNow;
            agreement.AgreementStatus = (int)AgreementStatus.Awaiting_Payment;

            // 6. Cập nhật DB (Không cần BeginTransaction vì chỉ tác động 1 bảng)
            await _agreementRepo.UpdateAsync(agreement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
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

        public async Task<Result<GhnFeeQuoteResponse>> PreviewShippingFeeAsync(
            Guid negotiationId,
            ShippingFeePreviewRequest request,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var negotiation = await _negotiationRepo.GetByIdAsync(negotiationId, cancellationToken);
            if (negotiation == null)
                return Result<GhnFeeQuoteResponse>.Fail(new Error("Negotiation.NotFound", "Không tìm thấy cuộc thương lượng."));

            bool isSeller = negotiation.SellerId == currentUserId;
            bool isBuyer = negotiation.BuyerId == currentUserId;

            if (!isSeller && !isBuyer)
                return Result<GhnFeeQuoteResponse>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền xem phí vận chuyển."));

            var post = await _postRepo.GetByIdAsync(negotiation.PostId, cancellationToken);
            if (post == null)
                return Result<GhnFeeQuoteResponse>.Fail(new Error("Post.NotFound", "Bài đăng không tồn tại."));

            var product = await _productRepo.GetDetailByPostIdAsync(post.PostId, cancellationToken);
            if (product == null)
                return Result<GhnFeeQuoteResponse>.Fail(new Error("Product.NotFound", "Không tìm thấy sản phẩm liên kết với bài đăng."));

            var quoteRequest = new GhnFeeQuoteRequest
            {
                FromDistrictId = request.FromDistrictId,
                FromWardCode = request.FromWardCode,
                ToDistrictId = request.ToDistrictId,
                ToWardCode = request.ToWardCode,
                ServiceTypeId = request.ServiceTypeId,

                // Tự động lấy thông số kiện hàng từ Product để FE không cần nhập
                WeightGram = product.Weight > 0 ? (int)(product.Weight.Value * 1000) : null,
                LengthCm = product.Length > 0 ? (int)product.Length.Value : null,
                WidthCm = product.Width > 0 ? (int)product.Width.Value : null,
                HeightCm = product.Height > 0 ? (int)product.Height.Value : null
            };

            try
            {
                var quote = await _ghnService.GetShippingFeeAsync(quoteRequest, cancellationToken);
                return Result<GhnFeeQuoteResponse>.Success(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tính phí vận chuyển thất bại cho NegotiationId {NegotiationId}.", negotiationId);
                return Result<GhnFeeQuoteResponse>.Fail(new Error("Ghn.CalculateFeeFailed", ex.Message));
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
