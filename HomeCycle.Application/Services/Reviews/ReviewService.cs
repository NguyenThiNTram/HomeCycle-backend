using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Reviews;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Reviews;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Profiles;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Reviews;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Reviews
{
    public class ReviewService : IReviewService
    {
        private static readonly TimeSpan EditWindow = TimeSpan.FromDays(3);

        private const string ReviewMediaTargetType = "Review";
        private const string ReviewMediaFolder = "reviews";

        private readonly IReviewRepository _reviewRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IUserRepository _userRepo;
        private readonly IPersonalProfileRepository _personalProfileRepo;
        private readonly IBusinessProfileRepository _businessProfileRepo;
        private readonly IMediaService _mediaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreateReviewRequest> _createValidator;
        private readonly IValidator<UpdateReviewRequest> _updateValidator;

        public ReviewService(
            IReviewRepository reviewRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IUserRepository userRepo,
            IPersonalProfileRepository personalProfileRepo,
            IBusinessProfileRepository businessProfileRepo,
            IMediaService mediaService,
            IUnitOfWork unitOfWork,
            IValidator<CreateReviewRequest> createValidator,
            IValidator<UpdateReviewRequest> updateValidator)
        {
            _reviewRepo = reviewRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _userRepo = userRepo;
            _personalProfileRepo = personalProfileRepo;
            _businessProfileRepo = businessProfileRepo;
            _mediaService = mediaService;
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        //public async Task<Result<ReviewResponseDto>> CreateReviewAsync(
        //    Guid orderId, CreateReviewRequest request, Guid currentUserId, CancellationToken ct = default)
        //{
        //    var validationResult = await _createValidator.ValidateAsync(request, ct);
        //    if (!validationResult.IsValid)
        //    {
        //        var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
        //        return Result<ReviewResponseDto>.Fail(new Error("Validation.InvalidRequest", errorMessage));
        //    }

        //    var order = await _orderRepo.GetByIdAsync(orderId, ct);
        //    if (order == null)
        //        return Result<ReviewResponseDto>.Fail(new Error("Order.NotFound", "Không tìm thấy đơn hàng."));

        //    var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);
        //    if (agreement == null)
        //        return Result<ReviewResponseDto>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận gắn với đơn hàng."));

        //    bool isBuyer = agreement.BuyerId == currentUserId;
        //    bool isSeller = agreement.SellerId == currentUserId;

        //    if (!isBuyer && !isSeller)
        //        return Result<ReviewResponseDto>.Fail(new Error("Auth.Forbidden", "Bạn không thuộc phiên giao dịch này nên không thể đánh giá."));

        //    if (order.OrderStatus != (int)OrderStatus.Completed)
        //        return Result<ReviewResponseDto>.Fail(new Error("Order.NotCompleted", "Chỉ có thể đánh giá sau khi đơn hàng hoàn thành."));

        //    var revieweeId = isBuyer ? agreement.SellerId : agreement.BuyerId;

        //    if (await _reviewRepo.ExistsAsync(orderId, currentUserId, ct))
        //        return Result<ReviewResponseDto>.Fail(new Error("Review.AlreadyExists", "Bạn đã đánh giá đơn hàng này."));

        //    var now = DateTime.UtcNow;
        //    var review = new review
        //    {
        //        ReviewId = Guid.NewGuid(),
        //        OrderId = orderId,
        //        ReviewerId = currentUserId,
        //        RevieweeId = revieweeId,
        //        Rating = request.Rating,
        //        Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
        //        ReviewStatus = (int)ReviewStatus.Active,
        //        CreatedAt = now,
        //        UpdatedAt = now
        //    };

        //    await _unitOfWork.BeginTransactionAsync(ct);
        //    try
        //    {
        //        await _reviewRepo.AddAsync(review, ct);
        //        await _unitOfWork.SaveChangesAsync(ct);

        //        if (request.Images != null && request.Images.Any(f => f != null && f.Length > 0))
        //        {
        //            var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
        //                targetId: review.ReviewId,
        //                targetType: ReviewMediaTargetType,
        //                folderName: ReviewMediaFolder,
        //                files: request.Images,
        //                cancellationToken: ct);

        //            if (!mediaResult.IsSuccess)
        //            {
        //                await _unitOfWork.RollbackTransactionAsync(ct);
        //                return Result<ReviewResponseDto>.Fail(mediaResult.Error!);
        //            }
        //        }

        //        await RecalculateReputationAsync(revieweeId, ct);

        //        await _unitOfWork.CommitTransactionAsync(ct);
        //    }
        //    catch
        //    {
        //        await _unitOfWork.RollbackTransactionAsync(ct);
        //        throw;
        //    }

        //    return Result<ReviewResponseDto>.Success(await BuildResponseAsync(review, ct));
        //}

        //public async Task<Result<ReviewResponseDto>> UpdateReviewAsync(
        //    Guid reviewId, UpdateReviewRequest request, Guid currentUserId, CancellationToken ct = default)
        //{
        //    var validationResult = await _updateValidator.ValidateAsync(request, ct);
        //    if (!validationResult.IsValid)
        //    {
        //        var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
        //        return Result<ReviewResponseDto>.Fail(new Error("Validation.InvalidRequest", errorMessage));
        //    }

        //    var review = await _reviewRepo.GetByIdAsync(reviewId, ct);
        //    if (review == null)
        //        return Result<ReviewResponseDto>.Fail(new Error("Review.NotFound", "Không tìm thấy đánh giá."));

        //    if (review.ReviewerId != currentUserId)
        //        return Result<ReviewResponseDto>.Fail(new Error("Auth.Forbidden", "Bạn chỉ có thể chỉnh sửa đánh giá của chính mình."));

        //    if (DateTime.UtcNow > review.CreatedAt.Add(EditWindow))
        //        return Result<ReviewResponseDto>.Fail(new Error("Review.EditWindowExpired", "Đánh giá chỉ có thể chỉnh sửa trong 3 ngày kể từ khi gửi."));

        //    review.Rating = request.Rating;
        //    review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        //    review.ReviewStatus = (int)ReviewStatus.Edited;
        //    review.UpdatedAt = DateTime.UtcNow;

        //    await _unitOfWork.BeginTransactionAsync(ct);
        //    try
        //    {
        //        await _reviewRepo.UpdateAsync(review, ct);
        //        await _unitOfWork.SaveChangesAsync(ct);

        //        await RecalculateReputationAsync(review.RevieweeId, ct);

        //        await _unitOfWork.CommitTransactionAsync(ct);
        //    }
        //    catch
        //    {
        //        await _unitOfWork.RollbackTransactionAsync(ct);
        //        throw;
        //    }

        //    return Result<ReviewResponseDto>.Success(await BuildResponseAsync(review, ct));
        //}
        public async Task<Result<ReviewResponseDto>> CreateReviewAsync(
            Guid orderId,
            CreateReviewRequest request,
            Guid currentUserId,
            CancellationToken ct = default)
        {
            var validationResult = await _createValidator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<ReviewResponseDto>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            var order = await _orderRepo.GetByIdAsync(orderId, ct);

            if (order == null)
                return Result<ReviewResponseDto>.Fail(new Error("Order.NotFound", "Không tìm thấy đơn hàng."));

            var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

            if (agreement == null)
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Agreement.NotFound",
                        "Không tìm thấy thỏa thuận gắn với đơn hàng."));
            }

            var isBuyer = agreement.BuyerId == currentUserId;
            var isSeller = agreement.SellerId == currentUserId;

            if (!isBuyer && !isSeller)
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Auth.Forbidden",
                        "Bạn không thuộc phiên giao dịch này nên không thể đánh giá."));
            }

            if (order.OrderStatus != (int)OrderStatus.Completed)
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Order.NotCompleted",
                        "Chỉ có thể đánh giá sau khi đơn hàng hoàn thành."));
            }

            var revieweeId = isBuyer
                ? agreement.SellerId
                : agreement.BuyerId;

            if (await _reviewRepo.ExistsAsync(orderId, currentUserId, ct))
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Review.AlreadyExists",
                        "Bạn đã đánh giá đơn hàng này."));
            }

            var now = DateTime.UtcNow;

            var review = new review
            {
                ReviewId = Guid.NewGuid(),
                OrderId = orderId,
                ReviewerId = currentUserId,
                RevieweeId = revieweeId,
                Rating = request.Rating,
                Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? null
                    : request.Comment.Trim(),
                ReviewStatus = (int)ReviewStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var (personalProfile, businessProfile) =
                    await GetReputationProfileForUpdateAsync(revieweeId, ct);

                if (personalProfile == null && businessProfile == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ReviewResponseDto>.Fail(ProfileErrors.ProfileNotFound);
                }

                var reviewScoreBefore = ReputationScoreCalculator.Calculate(
                    await _reviewRepo.GetValidReviewsByRevieweeAsync(revieweeId, ct));

                await _reviewRepo.AddAsync(review, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                if (request.Images != null &&
                    request.Images.Any(file => file != null && file.Length > 0))
                {
                    var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                        targetId: review.ReviewId,
                        targetType: ReviewMediaTargetType,
                        folderName: ReviewMediaFolder,
                        files: request.Images,
                        cancellationToken: ct);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<ReviewResponseDto>.Fail(mediaResult.Error!);
                    }
                }

                var reviewScoreAfter = ReputationScoreCalculator.Calculate(
                    await _reviewRepo.GetValidReviewsByRevieweeAsync(revieweeId, ct));

                await ApplyReviewScoreDeltaAsync(
                    personalProfile,
                    businessProfile,
                    reviewScoreAfter - reviewScoreBefore,
                    ct);

                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }

            return Result<ReviewResponseDto>.Success(
                await BuildResponseAsync(review, ct));
        }

        public async Task<Result<ReviewResponseDto>> UpdateReviewAsync(
            Guid reviewId,
            UpdateReviewRequest request,
            Guid currentUserId,
            CancellationToken ct = default)
        {
            var validationResult = await _updateValidator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<ReviewResponseDto>.Fail(new Error("Validation.InvalidRequest", errorMessage));
            }

            var review = await _reviewRepo.GetByIdAsync(reviewId, ct);

            if (review == null)
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Review.NotFound",
                        "Không tìm thấy đánh giá."));
            }

            if (review.ReviewerId != currentUserId)
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Auth.Forbidden",
                        "Bạn chỉ có thể chỉnh sửa đánh giá của chính mình."));
            }

            if (DateTime.UtcNow > review.CreatedAt.Add(EditWindow))
            {
                return Result<ReviewResponseDto>.Fail(
                    new Error(
                        "Review.EditWindowExpired",
                        "Đánh giá chỉ có thể chỉnh sửa trong 3 ngày kể từ khi gửi."));
            }

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var (personalProfile, businessProfile) =
                    await GetReputationProfileForUpdateAsync(review.RevieweeId, ct);

                if (personalProfile == null && businessProfile == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ReviewResponseDto>.Fail(ProfileErrors.ProfileNotFound);
                }

                var reviewScoreBefore = ReputationScoreCalculator.Calculate(
                    await _reviewRepo.GetValidReviewsByRevieweeAsync(review.RevieweeId, ct));

                review.Rating = request.Rating;
                review.Comment = string.IsNullOrWhiteSpace(request.Comment)
                    ? null
                    : request.Comment.Trim();
                review.ReviewStatus = (int)ReviewStatus.Edited;
                review.UpdatedAt = DateTime.UtcNow;

                await _reviewRepo.UpdateAsync(review, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var reviewScoreAfter = ReputationScoreCalculator.Calculate(
                    await _reviewRepo.GetValidReviewsByRevieweeAsync(review.RevieweeId, ct));

                await ApplyReviewScoreDeltaAsync(
                    personalProfile,
                    businessProfile,
                    reviewScoreAfter - reviewScoreBefore,
                    ct);

                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }

            return Result<ReviewResponseDto>.Success(
                await BuildResponseAsync(review, ct));
        }
        public async Task<Result<ReviewResponseDto>> GetByIdAsync(Guid reviewId, CancellationToken ct = default)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId, ct);
            if (review == null)
                return Result<ReviewResponseDto>.Fail(new Error("Review.NotFound", "Không tìm thấy đánh giá."));

            return Result<ReviewResponseDto>.Success(await BuildResponseAsync(review, ct));
        }

        public async Task<Result<ReviewResponseDto>> GetMyReviewForOrderAsync(
            Guid orderId, Guid currentUserId, CancellationToken ct = default)
        {
            var review = await _reviewRepo.GetByOrderAndReviewerAsync(orderId, currentUserId, ct);
            if (review == null)
                return Result<ReviewResponseDto>.Fail(new Error("Review.NotFound", "Bạn chưa đánh giá đơn hàng này."));

            return Result<ReviewResponseDto>.Success(await BuildResponseAsync(review, ct));
        }

        public async Task<Result<PagedResult<ReviewResponseDto>>> GetReviewsByUserAsync(
            Guid userId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var paged = await _reviewRepo.GetPagedByRevieweeAsync(userId, pageNumber, pageSize, ct);
            MarkCanEdit(paged.Items);
            await AttachImagesAsync(paged.Items, ct);
            return Result<PagedResult<ReviewResponseDto>>.Success(paged);
        }

        public async Task<Result<PagedResult<ReviewResponseDto>>> GetReviewsByOrderAsync(
            Guid orderId, Guid currentUserId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, ct);
            if (order == null)
                return Result<PagedResult<ReviewResponseDto>>.Fail(new Error("Order.NotFound", "Không tìm thấy đơn hàng."));

            var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);
            if (agreement == null)
                return Result<PagedResult<ReviewResponseDto>>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận gắn với đơn hàng."));

            if (agreement.BuyerId != currentUserId && agreement.SellerId != currentUserId)
                return Result<PagedResult<ReviewResponseDto>>.Fail(new Error("Auth.Forbidden", "Bạn không thuộc phiên giao dịch này."));

            var paged = await _reviewRepo.GetPagedByOrderAsync(orderId, pageNumber, pageSize, ct);
            MarkCanEdit(paged.Items);
            await AttachImagesAsync(paged.Items, ct);
            return Result<PagedResult<ReviewResponseDto>>.Success(paged);
        }

        private async Task RecalculateReputationAsync(Guid userId, CancellationToken ct)
        {
            var validReviews = await _reviewRepo.GetValidReviewsByRevieweeAsync(userId, ct);
            var newScore = ReputationScoreCalculator.Calculate(validReviews);

            var user = await _userRepo.GetByIdAsync(userId, ct);
            if (user == null)
                return;

            if (user.Role == UserRole.Business)
            {
                var business = await _businessProfileRepo.GetByUserIdAsync(userId, ct);
                if (business == null)
                    return;

                business.ReputationScore = newScore;
                _businessProfileRepo.Update(business);
            }
            else
            {
                var personal = await _personalProfileRepo.GetByUserIdAsync(userId, ct);
                if (personal == null)
                    return;

                personal.ReputationScore = newScore;
                await _personalProfileRepo.UpdateAsync(personal, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        private async Task<ReviewResponseDto> BuildResponseAsync(review review, CancellationToken ct)
        {
            var reviewer = await _userRepo.GetByIdAsync(review.ReviewerId, ct);
            var reviewee = await _userRepo.GetByIdAsync(review.RevieweeId, ct);

            var response = new ReviewResponseDto
            {
                ReviewId = review.ReviewId,
                OrderId = review.OrderId,
                ReviewerId = review.ReviewerId,
                RevieweeId = review.RevieweeId,
                Rating = review.Rating,
                Comment = review.Comment,
                ReviewStatus = review.ReviewStatus,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                CanEdit = DateTime.UtcNow <= review.CreatedAt.Add(EditWindow),
                ReviewerName = reviewer?.Username,
                ReviewerAvatarUrl = reviewer?.AvatarUrl,
                RevieweeName = reviewee?.Username,
                RevieweeAvatarUrl = reviewee?.AvatarUrl
            };

            var mediaResult = await _mediaService.GetByTargetsAsync(new[] { review.ReviewId }, ReviewMediaTargetType, ct);
            if (mediaResult.IsSuccess && mediaResult.Data != null
                && mediaResult.Data.TryGetValue(review.ReviewId, out var images))
            {
                response.Images = images;
            }

            return response;
        }

        private async Task AttachImagesAsync(IEnumerable<ReviewResponseDto> items, CancellationToken ct)
        {
            var list = items.ToList();
            if (list.Count == 0)
                return;

            var reviewIds = list.Select(x => x.ReviewId).Distinct().ToArray();

            var mediaResult = await _mediaService.GetByTargetsAsync(reviewIds, ReviewMediaTargetType, ct);
            if (!mediaResult.IsSuccess || mediaResult.Data == null)
                return;

            foreach (var item in list)
            {
                if (mediaResult.Data.TryGetValue(item.ReviewId, out var images))
                    item.Images = images;
            }
        }

        private void MarkCanEdit(IEnumerable<ReviewResponseDto> items)
        {
            foreach (var item in items)
                item.CanEdit = DateTime.UtcNow <= item.CreatedAt.Add(EditWindow);
        }

        private async Task<(personal_profile? Personal, business_profile? Business)> GetReputationProfileForUpdateAsync(
            Guid userId,
            CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(userId, ct);

            if (user == null)
                return (null, null);

            if (user.Role == UserRole.Business)
            {
                var business = await _businessProfileRepo
                    .GetByUserIdForUpdateAsync(userId, ct);

                return (null, business);
            }

            if (user.Role == UserRole.Personal)
            {
                var personal = await _personalProfileRepo
                    .GetByUserIdForUpdateAsync(userId, ct);

                return (personal, null);
            }

            return (null, null);
        }

        private async Task ApplyReviewScoreDeltaAsync(
            personal_profile? personalProfile,
            business_profile? businessProfile,
            int pointDelta,
            CancellationToken ct)
        {
            if (pointDelta == 0)
                return;

            if (businessProfile != null)
            {
                businessProfile.ReputationScore =
                    ReputationScoreCalculator.ApplyDelta(
                        businessProfile.ReputationScore,
                        pointDelta);

                _businessProfileRepo.Update(businessProfile);
            }
            else if (personalProfile != null)
            {
                personalProfile.ReputationScore =
                    ReputationScoreCalculator.ApplyDelta(
                        personalProfile.ReputationScore,
                        pointDelta);

                await _personalProfileRepo.UpdateAsync(personalProfile, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
