using FluentValidation;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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
        private readonly IPostRepository _postRepo;
        private readonly IOfferRepository _offerRepo;
        private readonly IProductRepository _productRepo;
        private readonly IValidator<CreateAgreementFormRequest> _createValidator;
        private readonly IValidator<UpdateAgreementFormRequest> _updateValidator;
        public AgreementFormService(
            IUnitOfWork unitOfWork,
            IAgreementFormRepository agreementRepo,
            INegotiationRepository negotiationRepo,
            IPostRepository postRepo,
            IOfferRepository offerRepo,
            IProductRepository productRepo,
            IValidator<CreateAgreementFormRequest> createValidator,
            IValidator<UpdateAgreementFormRequest> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _agreementRepo = agreementRepo;
            _negotiationRepo = negotiationRepo;
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

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

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

    }
}
