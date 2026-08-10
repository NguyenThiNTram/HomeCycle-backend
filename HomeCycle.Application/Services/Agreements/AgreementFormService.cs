using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
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

                var product = await _productRepo.GetByPostIdAsync(post.PostId, cancellationToken);
                if (product == null)
                    return Result<Guid>.Fail(new Error("Product.NotFound", "Không tìm thấy sản phẩm liên kết với bài đăng."));

                var pSnapshot = new
                {
                    PostInfo = new
                    {
                        PostId = post.PostId,
                        Description = post.Description,
                        BasePrice = post.BasePrice,
                        PostType = post.PostType
                    },
                    ProductInfo = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        BrandName = product.BrandName,
                        OriginalPrice = product.OriginalPrice,
                        Condition = product.FunctionalityStatus // Lưu lại tình trạng thực tế lúc chốt
                        // Tùy chọn: Include thêm danh sách Product_Attribute_Value nếu hệ thống yêu cầu gắt gao về bằng chứng pháp lý
                    }
                };

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

                    PSnapshot = JsonSerializer.Serialize(pSnapshot),
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
    }
}
