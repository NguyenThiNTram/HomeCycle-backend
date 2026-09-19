using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.DTOs.Responses.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Services;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Application.Interfaces.Services.Profiles;
using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Commons.Audits;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Application.Services.Posts
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IOfferRepository _offerRepository;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IValidator<CreateSellPostRequest> _createSellValidator;
        private readonly IValidator<CreateBuyPostRequest> _createBuyValidator;
        private readonly IValidator<UpdateSellPostRequest> _updateSellValidator;
        private readonly IValidator<UpdateBuyPostRequest> _updateBuyValidator;
        private readonly IValidator<PostSearchRequest> _searchValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly IAuditService _auditService;
        private readonly ISupplierMatchService _supplierMatchService;
        private readonly ILogger<PostService> _logger;
        private readonly IBusinessProfileService _businessProfileService;

        private const string PostMediaTargetType = "Post";
        private const string PostMediaFolder = "posts";

        public PostService(
            IPostRepository postRepository,
            IOfferRepository offerRepository,
            IProductService productService,
            IMediaService mediaService,
            IUserRepository userRepository,
            IReviewRepository reviewRepository,
            IValidator<CreateSellPostRequest> createSellValidator,
            IValidator<CreateBuyPostRequest> createBuyValidator,
            IValidator<UpdateSellPostRequest> updateSellValidator,
            IValidator<UpdateBuyPostRequest> updateBuyValidator,
            IValidator<PostSearchRequest> searchValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IPlatformPolicyProvider platformPolicyProvider,
            IAuditService auditService,
            ISupplierMatchService supplierMatchService,
            ILogger<PostService> logger,
            IBusinessProfileService businessProfileService)
        {
            _postRepository = postRepository;
            _offerRepository = offerRepository;
            _productService = productService;
            _mediaService = mediaService;
            _userRepository = userRepository;
            _reviewRepository = reviewRepository;
            _createSellValidator = createSellValidator;
            _createBuyValidator = createBuyValidator;
            _updateSellValidator = updateSellValidator;
            _updateBuyValidator = updateBuyValidator;
            _searchValidator = searchValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _platformPolicyProvider = platformPolicyProvider;
            _auditService = auditService;
            _supplierMatchService = supplierMatchService;
            _logger = logger;
            _businessProfileService = businessProfileService;
        }

        // ================== CREATE - SELL ==================

        public async Task<Result<PostResponse>> CreateSellPostAsync(
            Guid ownerId, CreateSellPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createSellValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)), cancellationToken);
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var roleError = await ValidateCreateRoleAsync(ownerId, UserRole.Personal, cancellationToken);
            if (roleError is not null)
            {
                await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + roleError.Message, cancellationToken);
                return Result<PostResponse>.Fail(roleError);
            }

            if (request.Medias == null || !request.Medias.Any())
            {
                var error = "Bài đăng bán bắt buộc phải có ít nhất 1 hình ảnh sản phẩm.";
                await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + error, cancellationToken);
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(error));
            }

            var now = DateTime.UtcNow;
            var post = _mapper.Map<post>(request);

            post.PostId = Guid.NewGuid();
            post.OwnerId = ownerId;
            post.PostType = PostType.Sell;
            post.BasePrice = request.BasePrice;
            post.CreatedAt = now;
            post.UpdatedAt = now;
            post.RemainingQuantity = request.Quantity;
            post.Status = PostStatus.Active;
            post.ExpiryDate = now.AddMonths(12);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _postRepository.AddAsync(post, cancellationToken);

                var productResult = await _productService.PrepareForCreateAsync(post.PostId, request.Product, cancellationToken);
                if (!productResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + productResult.Error!.Message, cancellationToken);
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                    targetId: post.PostId,
                    targetType: PostMediaTargetType,
                    folderName: PostMediaFolder,
                    files: request.Medias,
                    uploadContext: FileUploadContext.PostMedia,
                    cancellationToken: cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + mediaResult.Error!.Message, cancellationToken);
                    return Result<PostResponse>.Fail(mediaResult.Error!);
                }

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.PostCreate,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = ownerId,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = post.PostId,
                    NewValues = new Dictionary<string, object?>
                    {
                        ["postType"] = post.PostType.ToString(),
                        ["status"] = post.Status.ToString(),
                        ["basePrice"] = post.BasePrice,
                        ["quantity"] = post.Quantity
                    }
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(post);

                // Gửi thông báo realtime khi tạo bài đăng thành công
                await SendPostCreatedNotificationAsync(ownerId, post.PostId, "Bài đăng bán đã được tạo thành công", "Bài đăng bán của bạn đã được đăng tải.", cancellationToken);

                return Result<PostResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                await SendPostCreationFailedNotificationAsync(ownerId, "Tạo bài đăng bán thất bại: " + ex.Message, cancellationToken);
                throw;
            }
        }

        // ================== UPDATE - SELL ==================

        public async Task<Result<PostResponse>> UpdateSellPostAsync(
            Guid ownerId, Guid postId, UpdateSellPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateSellValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var existing = await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);

                var checkError = ValidateOwnershipAndComputeRemaining(
                    existing, ownerId, PostType.Sell, request.Quantity, out int newRemainingQuantity, out bool isRestock);
                if (checkError is not null)
                    return Result<PostResponse>.Fail(checkError);

                var reserved = await _postRepository.GetReservedQuantityAsync(postId, null, cancellationToken);
                if (newRemainingQuantity < reserved)
                    return Result<PostResponse>.Fail(PostErrors.InvalidUpdateQuantity(
                        (isRestock ? 0 : existing!.Quantity - existing.RemainingQuantity) + reserved,
                        request.Quantity ?? existing!.Quantity));
                var previousPrice = existing!.BasePrice;
                var previousQuantity = existing.Quantity;
                var previousRemainingQuantity = existing.RemainingQuantity;
                var previousPostStatus = existing.Status;
                _mapper.Map(request, existing);
                existing.Quantity = request.Quantity ?? previousQuantity;
                existing.BasePrice = request.BasePrice ?? previousPrice;
                existing.RemainingQuantity = newRemainingQuantity;
                existing.UpdatedAt = DateTime.UtcNow;

                if (existing.RemainingQuantity == 0)
                {
                    existing.Status = PostStatus.Closed;
                    await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);
                }
                else if (existing.Status == PostStatus.Closed && (isRestock || existing.Quantity > previousQuantity))
                {
                    existing.Status = PostStatus.Active;
                }
                await _postRepository.UpdateAsync(existing, cancellationToken);

                var productResult = await _productService.PrepareForUpdateAsync(postId, request.Product, cancellationToken);
                if (!productResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                // Kiểm tra xem request có chứa danh sách ảnh mới không
                if (request.Medias != null && request.Medias.Any())
                {
                    //var mediaResult = await _mediaService.ReplaceMediaAsync(
                    //    postId, PostMediaTargetType, PostMediaFolder, request.Medias, cancellationToken);
                    var mediaResult = await _mediaService.ReplaceMediaAsync(
                        targetId: postId,
                        targetType: PostMediaTargetType,
                        folderName: PostMediaFolder,
                        files: request.Medias,
                        uploadContext: FileUploadContext.PostMedia,
                        cancellationToken: cancellationToken);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PostResponse>.Fail(mediaResult.Error!);
                    }
                }
                // Nếu không có Medias trong request -> Bỏ qua, giữ nguyên ảnh hiện tại trong DB.

                var sellPostAuditDiff = new AuditDiffBuilder()
                    .Add("basePrice", previousPrice, existing.BasePrice)
                    .Add("quantity", previousQuantity, existing.Quantity)
                    .Add("remainingQuantity", previousRemainingQuantity, existing.RemainingQuantity)
                    .Add("status", previousPostStatus.ToString(), existing.Status.ToString());

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.PostUpdate,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = ownerId,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = existing.PostId,
                    OldValues = sellPostAuditDiff.OldValues,
                    NewValues = sellPostAuditDiff.NewValues
                }, cancellationToken);


                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(existing);
                return Result<PostResponse>.Success(response);
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        // ================== QUERY + SEARCH + HELPERS ==================
        public async Task<Result<PostDetailResponse>> GetDetailAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _postRepository.GetDetailByIdAsync(postId, cancellationToken);
            if (entity is null)
                return Result<PostDetailResponse>.Fail(PostErrors.NotFound);

            if (entity is null || entity.Status == PostStatus.Deleted)
                return Result<PostDetailResponse>.Fail(PostErrors.NotFound);

            var response = _mapper.Map<PostDetailResponse>(entity);

            var validReviews = await _reviewRepository.GetValidReviewsByRevieweeAsync(entity.OwnerId, cancellationToken);
            var ratingPolicy = await _platformPolicyProvider.GetRatingConfigAsync(cancellationToken);

            response.AverageRating = ReputationScoreCalculator.CalculateDisplayStarRating(validReviews, ratingPolicy);
            response.TotalReviews = validReviews.Count;

            var productResult = await _productService.GetDetailByPostIdAsync(postId, cancellationToken);

            var mediaResult =  await _mediaService.GetByTargetsAsync(new[] { postId }, PostMediaTargetType, cancellationToken);

            if (!productResult.IsSuccess || productResult.Data is null)
                return Result<PostDetailResponse>.Fail(ProductErrors.ProductNotFound);

            if (!mediaResult.IsSuccess)
                return Result<PostDetailResponse>.Fail(mediaResult.Error!);

            response.Product = productResult.Data;
            response.Medias = mediaResult.Data.TryGetValue(
                postId,
                out var postMedias)
                    ? postMedias
                    : Array.Empty<MediaResponse>();

            return Result<PostDetailResponse>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> GetAllAsync(
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var paged = await _postRepository.GetAllAsync(request, cancellationToken);

            var items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList();

            var postIds = items.Select(x => x.PostId).Distinct().ToArray();

            var mediaResult = await _mediaService.GetByTargetsAsync(postIds, PostMediaTargetType, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
            {
                return Result<PagedResult<PostResponse>>.Fail(
                    mediaResult.Error!);
            }

            foreach (var item in items)
            {
                item.Medias = mediaResult.Data.TryGetValue(
                    item.PostId,
                    out var medias)
                        ? medias
                        : Array.Empty<MediaResponse>();
            }

            var response = new PagedResult<PostResponse>
            {
                Items = items,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> GetAllActiveAsync(
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var paged = await _postRepository.GetAllActiveAsync(request, cancellationToken);

            var items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList();

            var postIds = items.Select(x => x.PostId).Distinct().ToArray();

            var mediaResult = await _mediaService.GetByTargetsAsync(postIds, PostMediaTargetType, cancellationToken);

            if (!mediaResult.IsSuccess || mediaResult.Data is null)
            {
                return Result<PagedResult<PostResponse>>.Fail(
                    mediaResult.Error!);
            }

            foreach (var item in items)
            {
                item.Medias = mediaResult.Data.TryGetValue(
                    item.PostId,
                    out var medias)
                        ? medias
                        : Array.Empty<MediaResponse>();
            }

            var response = new PagedResult<PostResponse>
            {
                Items = items,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> GetAllByOwnerAsync(
            Guid ownerId,
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            //var paged = await _postRepository.GetAllByOwnerAsync(ownerId, request, cancellationToken);

            //var response = new PagedResult<PostResponse>
            //{
            //    Items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList(),
            //    PageNumber = paged.PageNumber,
            //    PageSize = paged.PageSize,
            //    TotalCount = paged.TotalCount
            //};

            //return Result<PagedResult<PostResponse>>.Success(response);

            var paged = await _postRepository.GetAllByOwnerAsync(ownerId,  request, cancellationToken);

            var items = paged.Items
                .Select(x => _mapper.Map<PostResponse>(x))
                .ToList();

            if (items.Count > 0)
            {
                var postIds = items
                    .Select(x => x.PostId)
                    .Distinct()
                    .ToArray();

                var mediaResult = await _mediaService.GetByTargetsAsync(
                    postIds,
                    PostMediaTargetType,
                    cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    return Result<PagedResult<PostResponse>>.Fail(
                        mediaResult.Error!);
                }

                var mediasByPost = mediaResult.Data
                    ?? new Dictionary<Guid, IReadOnlyList<MediaResponse>>();

                foreach (var item in items)
                {
                    item.Medias = mediasByPost.TryGetValue(
                        item.PostId,
                        out var medias)
                            ? medias
                            : Array.Empty<MediaResponse>();
                }
            }

            var response = new PagedResult<PostResponse>
            {
                Items = items,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PostDetailResponse>> GetDetailByOwnerAsync(
            Guid ownerId,
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _postRepository.GetDetailByOwnerAsync(ownerId, postId, cancellationToken);
            if (entity is null || entity.Status == PostStatus.Deleted)
                return Result<PostDetailResponse>.Fail(PostErrors.NotFound);

            var response = _mapper.Map<PostDetailResponse>(entity);

            var productResult = await _productService.GetDetailByPostIdAsync(postId, cancellationToken);
            var mediaResult = await _mediaService.GetByTargetsAsync(new[] { postId }, PostMediaTargetType, cancellationToken);

            if (!productResult.IsSuccess || productResult.Data is null)
                return Result<PostDetailResponse>.Fail(ProductErrors.ProductNotFound);

            if (!mediaResult.IsSuccess)
                return Result<PostDetailResponse>.Fail(mediaResult.Error!);

            response.Product = productResult.Data;
            response.Medias = mediaResult.Data.TryGetValue(
                postId,
                out var postMedias)
                    ? postMedias
                    : Array.Empty<MediaResponse>();

            return Result<PostDetailResponse>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> DiscoverBusinessAsync(
            Guid userId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            if ((long)(request.PageNumber - 1) * request.PageSize > int.MaxValue)
                return Result<PagedResult<PostResponse>>.Fail(
                    ValidationErrors.InvalidRequest("Số trang vượt quá giới hạn hỗ trợ."));

            var survey = await _businessProfileService.GetProcurementPreferenceAsync(userId, cancellationToken);
            if (!survey.IsSuccess || survey.Data is null)
            {
                if (survey.Error?.Code is "Survey.NotFound" or "BusinessProfile.NotFound")
                    return Result<PagedResult<PostResponse>>.Fail(
                        new Error("SURVEY_REQUIRED", "Vui lòng hoàn tất khảo sát doanh nghiệp để khám phá bài đăng phù hợp."));
                return Result<PagedResult<PostResponse>>.Fail(survey.Error!);
            }

            var paged = await _postRepository.DiscoverBusinessAsync(userId, survey.Data, request, cancellationToken);
            return await MapPostPageAsync(paged, cancellationToken);
        }

        public async Task<Result<PagedResult<PostResponse>>> SearchAsync(
            PostSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PagedResult<PostResponse>>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var paged = await _postRepository.SearchAsync(request, cancellationToken);
            return await MapPostPageAsync(paged, cancellationToken);
        }

        private async Task<Result<PagedResult<PostResponse>>> MapPostPageAsync(
            PagedResult<post> paged, CancellationToken cancellationToken)
        {
            var items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList();

            if (items.Count > 0)
            {
                var postIds = items
                    .Select(x => x.PostId)
                    .Distinct()
                    .ToArray();

                var mediaResult = await _mediaService.GetByTargetsAsync(
                    postIds,
                    PostMediaTargetType,
                    cancellationToken);

                if (!mediaResult.IsSuccess)
                    return Result<PagedResult<PostResponse>>.Fail(mediaResult.Error!);

                var mediasByPost = mediaResult.Data ?? new Dictionary<Guid, IReadOnlyList<MediaResponse>>();

                foreach (var item in items)
                {
                    item.Medias = mediasByPost.TryGetValue(
                        item.PostId,
                        out var medias)
                            ? medias
                            : Array.Empty<MediaResponse>();
                }
            }

            var response = new PagedResult<PostResponse>
            {
                Items = items,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public Task<Result<bool>> CloseAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default) =>
            ChangeLifecycleAsync(ownerId, postId, PostStatus.Closed, false, cancellationToken);
        public Task<Result<bool>> ReactivateAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default) =>
            ChangeLifecycleAsync(ownerId, postId, PostStatus.Active, false, cancellationToken);

        public async Task<Result<bool>> DeleteAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (existing is null)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (existing.PostType == PostType.Buy) return await DeleteBuyPostAsAdminAsync(postId, cancellationToken);
            if (await _postRepository.HasUnfinishedTransactionsAsync(postId, cancellationToken))
                return Result<bool>.Fail(ValidationErrors.InvalidRequest("Bài đăng đang có giao dịch chưa hoàn tất."));
            var deleted = await _postRepository.DeleteAsync(postId, cancellationToken);
            if (!deleted)
                return Result<bool>.Fail(PostErrors.NotFound);

            var hardDeletePostAuditDiff = new AuditDiffBuilder()
                .Add("exists", true, false);

            await _auditService.EnqueueAsync(new AuditEvent
            {
                Category = AuditCategory.Administration,
                Action = AuditActions.PostDelete,
                Outcome = AuditOutcome.Success,
                ActorType = AuditActorType.User,
                TargetType = AuditTargetTypes.Post,
                TargetId = existing.PostId,
                OldValues = hardDeletePostAuditDiff.OldValues,
                NewValues = hardDeletePostAuditDiff.NewValues,
                Metadata = new Dictionary<string, object?>
                {
                    ["postType"] = existing.PostType.ToString(),
                    ["previousStatus"] = existing.Status.ToString(),
                    ["hardDelete"] = true
                }
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        // ================== MODERATOR - SUSPEND ==================

        public async Task<Result<bool>> SuspendAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var existing = await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);
                if (existing is null || existing.Status == PostStatus.Deleted) return Result<bool>.Fail(PostErrors.NotFound);
                if (existing.Status == PostStatus.Suspended) return Result<bool>.Fail(PostErrors.PostAlreadySuspended);
                var previousPostStatus = existing.Status;
                existing.Status = PostStatus.Suspended;
                existing.UpdatedAt = DateTime.UtcNow;
                await _postRepository.UpdateAsync(existing, cancellationToken);
                await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);

                var suspendPostAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousPostStatus.ToString(), existing.Status.ToString());

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.PostSuspend,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = existing.PostId,
                    OldValues = suspendPostAuditDiff.OldValues,
                    NewValues = suspendPostAuditDiff.NewValues
                }, cancellationToken);


                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }

        private async Task<Error?> ValidateCreateRoleAsync(
            Guid ownerId, UserRole requiredRole, CancellationToken cancellationToken)
        {
            var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
            if (owner is null)
                return PostErrors.RoleNotAllowed;

            return owner.Status == UserStatus.Active && owner.Role == requiredRole
                ? null
                : PostErrors.RoleNotAllowed;
        }

        private Error? ValidateOwnershipAndComputeRemaining(
            post? existing, Guid ownerId, PostType postType, int? requestedQuantity,
            out int newRemainingQuantity, out bool isRestock)
        {
            newRemainingQuantity = 0;
            isRestock = false;

            if (existing is null)
                return PostErrors.NotFound;

            if (existing.OwnerId != ownerId)
                return PostErrors.Forbidden;

            if (existing.PostType != postType)
                return PostErrors.InvalidPostType;

            var newQuantity = requestedQuantity ?? existing.Quantity;
            // Only an explicit positive quantity can replenish an exhausted sell post.
            isRestock = postType == PostType.Sell && existing.RemainingQuantity == 0 && requestedQuantity is > 0;
            if (existing.Status is PostStatus.Deleted or PostStatus.Suspended ||
                (existing.Status == PostStatus.Closed && !isRestock && newQuantity <= existing.Quantity))
                return PostErrors.PostAlreadyClosedOrDeleted;

            // Spec: "Sửa hoặc xóa tin trong thời hạn cho phép"
            if (existing.ExpiryDate.HasValue && existing.ExpiryDate.Value < DateTime.UtcNow)
                return PostErrors.PostExpired;

            int soldQuantity = existing.Quantity - existing.RemainingQuantity;
            newRemainingQuantity = isRestock ? newQuantity : newQuantity - soldQuantity;

            if (newRemainingQuantity < 0)
                return PostErrors.InvalidUpdateQuantity(soldQuantity, newQuantity);

            return null;
        }

        private async Task SendPostCreatedNotificationAsync(Guid userId, Guid postId, string title, string message, CancellationToken cancellationToken)
        {
            try
            {
                var notification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        userId,
                        title,
                        message,
                        NotificationTargetType.Post,
                        postId),
                    cancellationToken);

                await _notificationService.PublishCreatedSafelyAsync(notification);
            }
            catch (Exception)
            {
                // Log warning but don't fail the main operation
            }
        }

        private async Task SendPostCreationFailedNotificationAsync(Guid userId, string message, CancellationToken cancellationToken)
        {
            try
            {
                var notification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        userId,
                        "Tạo bài đăng thất bại",
                        message,
                        NotificationTargetType.Post,
                        Guid.Empty),
                    cancellationToken);

                await _notificationService.PublishCreatedSafelyAsync(notification);
            }
            catch (Exception)
            {
                // Log warning but don't fail the main operation
            }
        }

        public async Task<Result<SellPostDetailResponse>> GetSellDetailAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var detail = await GetDetailAsync(postId, cancellationToken);
            if (!detail.IsSuccess) return Result<SellPostDetailResponse>.Fail(detail.Error!);
            if (detail.Data!.PostType != PostType.Sell) return Result<SellPostDetailResponse>.Fail(PostErrors.NotFound);
            return Result<SellPostDetailResponse>.Success(new SellPostDetailResponse(detail.Data));
        }

        public async Task<Result<BuyPostDetailResponse>> GetBuyDetailAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var detail = await GetDetailAsync(postId, cancellationToken);
            if (!detail.IsSuccess) return Result<BuyPostDetailResponse>.Fail(detail.Error!);
            if (detail.Data!.PostType != PostType.Buy) return Result<BuyPostDetailResponse>.Fail(PostErrors.NotFound);
            var agreed = await _postRepository.GetAgreedBuyQuantityAsync(postId, null, cancellationToken);
            return Result<BuyPostDetailResponse>.Success(new BuyPostDetailResponse(detail.Data, agreed));
        }

        public async Task<Result<PostResponse>> CreateBuyPostAsync(Guid ownerId, CreateBuyPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createBuyValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(validation.ToString()));
            var roleError = await ValidateCreateRoleAsync(ownerId, UserRole.Business, cancellationToken);
            if (roleError != null) return Result<PostResponse>.Fail(roleError);
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<post>(request);
            entity.PostId = Guid.NewGuid(); entity.OwnerId = ownerId;
            entity.Description = request.Description.Trim(); entity.PostType = PostType.Buy;
            entity.IsBusinessPosting = true; entity.Quantity = request.Quantity ?? 1;
            entity.RemainingQuantity = entity.Quantity; entity.CreatedAt = now; entity.UpdatedAt = now;
            entity.Status = PostStatus.Active; entity.ExpiryDate = request.ExpiryDate?.ToUniversalTime() ?? now.AddDays(30);
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _postRepository.AddAsync(entity, cancellationToken);
                var requirement = _mapper.Map<ProductRequirementRequest>(request);
                requirement.ProductName = request.Title.Trim(); requirement.AttributeValues ??= [];
                var product = await _productService.PrepareForRequirementAsync(entity.PostId, requirement, cancellationToken);
                if (!product.IsSuccess) return Result<PostResponse>.Fail(product.Error!);

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.PostCreate,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = ownerId,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = entity.PostId,
                    NewValues = new Dictionary<string, object?>
                    {
                        ["postType"] = entity.PostType.ToString(),
                        ["status"] = entity.Status.ToString(),
                        ["minExpectedPrice"] = entity.MinExpectedPrice,
                        ["basePrice"] = entity.BasePrice,
                        ["quantity"] = entity.Quantity
                    }
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
            var saved = await _postRepository.GetByIdAsync(entity.PostId, cancellationToken);
            await SendPostCreatedNotificationAsync(ownerId, entity.PostId, "Đã tạo tin thu mua", "Tin thu mua của bạn đã được đăng tải.", cancellationToken);
            await TryWarmSupplierMatchesAsync(ownerId, entity.PostId, cancellationToken);
            return Result<PostResponse>.Success(_mapper.Map<PostResponse>(saved));
        }

        public async Task<Result<PostResponse>> UpdateBuyPostAsync(Guid ownerId, Guid postId, UpdateBuyPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateBuyValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(validation.ToString()));
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);
                var current = await _postRepository.GetDetailByIdAsync(postId, cancellationToken);
                if (current == null) return Result<PostResponse>.Fail(PostErrors.NotFound);
                if (current.OwnerId != ownerId) return Result<PostResponse>.Fail(PostErrors.Forbidden);
                if (current.PostType != PostType.Buy) return Result<PostResponse>.Fail(PostErrors.InvalidPostType);
                if (current.Status is PostStatus.Deleted or PostStatus.Suspended ||
                    (current.Status == PostStatus.Closed && (request.Quantity ?? current.Quantity) <= current.Quantity))
                    return Result<PostResponse>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
                if (current.ExpiryDate <= DateTime.UtcNow) return Result<PostResponse>.Fail(PostErrors.PostExpired);
                if (current.Product == null) return Result<PostResponse>.Fail(ProductErrors.ProductNotFound);
                var p = current.Product!;
                var merged = new CreateBuyPostRequest {
                    Title = p.ProductName ?? "", Description = current.Description ?? "", CategoryId = p.CategoryId,
                    ProductTypeId = p.ProductTypeId, BrandId = p.BrandId, ModelNumber = p.ModelNumber,
                    FunctionalityStatus = p.FunctionalityStatus,
                    UsageDuration = p.UsageDuration, DamageLevel = p.DamageLevel, StreetAddress = current.StreetAddress,
                    Ward = current.Ward, City = current.City, PriorityLevel = current.PriorityLevel,
                    PriceFrom = current.MinExpectedPrice, PriceTo = current.BasePrice, Quantity = current.Quantity, ExpiryDate = current.ExpiryDate,
                    AttributeValues = p.Product_Attribute_Values.Select(v => new ProductAttributeValueRequest {
                        AttributeId = v.AttributeId, OptionId = v.OptionId, ValueText = v.ValueText, ValueNumber = v.ValueNumber, ValueBoolean = v.ValueBoolean
                    }).ToList()
                };
                var before = System.Text.Json.JsonSerializer.SerializeToElement(merged);
                var previousMinExpectedPrice = current.MinExpectedPrice;
                var previousBasePrice = current.BasePrice;
                var previousQuantity = current.Quantity;
                var previousPostStatus = current.Status;
                var previousExpiryDate = current.ExpiryDate;
                foreach (var name in request.ChangedProperties)
                    typeof(CreateBuyPostRequest).GetProperty(name)!.SetValue(merged, typeof(UpdateBuyPostRequest).GetProperty(name)!.GetValue(request));
                if (merged.CategoryId == null && !request.ChangedProperties.Contains(nameof(request.ProductTypeId))) merged.ProductTypeId = null;
                if (merged.ProductTypeId != p.ProductTypeId && !request.ChangedProperties.Contains(nameof(request.AttributeValues))) merged.AttributeValues = [];
                merged.AttributeValues ??= [];
                var effectiveValidation = await _createBuyValidator.ValidateAsync(merged, cancellationToken);
                if (!effectiveValidation.IsValid) return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest(effectiveValidation.ToString()));
                if (merged.ExpiryDate?.ToUniversalTime() > current.CreatedAt.AddMonths(6))
                    return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest("Thời hạn tối đa là 6 tháng từ ngày tạo tin."));
                var allocated = current.Quantity - current.RemainingQuantity;
                var reserved = await _postRepository.GetReservedQuantityAsync(postId, null, cancellationToken);
                var agreed = await _postRepository.GetAgreedBuyQuantityAsync(postId, null, cancellationToken);
                var minimumQuantity = Math.Max(allocated + reserved, agreed);
                if (merged.Quantity < minimumQuantity)
                    return Result<PostResponse>.Fail(PostErrors.InvalidUpdateQuantity(minimumQuantity, merged.Quantity!.Value));
                var materialFields = new[] { "Title", "BrandId", "CategoryId", "ProductTypeId", "ModelNumber", "FunctionalityStatus", "UsageDuration", "DamageLevel", "AttributeValues", "PriceFrom", "PriceTo" };
                var after = System.Text.Json.JsonSerializer.SerializeToElement(merged);
                var material = materialFields.Any(name => before.GetProperty(name).GetRawText() != after.GetProperty(name).GetRawText());
                if (material && await _postRepository.HasUnfinishedTransactionsAsync(postId, cancellationToken))
                    return Result<PostResponse>.Fail(ValidationErrors.InvalidRequest("Không thể đổi tiêu chí khi tin đang có giao dịch chưa hoàn tất."));
                current.Description = merged.Description.Trim(); current.Quantity = merged.Quantity!.Value;
                current.RemainingQuantity = current.Quantity - allocated;
                current.MinExpectedPrice = merged.PriceFrom; current.BasePrice = merged.PriceTo;
                current.StreetAddress = merged.StreetAddress; current.Ward = merged.Ward; current.City = merged.City;
                current.PriorityLevel = merged.PriorityLevel; current.ExpiryDate = merged.ExpiryDate?.ToUniversalTime(); current.UpdatedAt = DateTime.UtcNow;
                if (current.RemainingQuantity == 0 || agreed >= current.Quantity) current.Status = PostStatus.Closed;
                else if (current.Status == PostStatus.Closed && current.Quantity > previousQuantity)
                    current.Status = PostStatus.Active;
                await _postRepository.UpdateAsync(current, cancellationToken);
                var requirement = _mapper.Map<ProductRequirementRequest>(merged); requirement.ProductName = merged.Title.Trim();
                // Merge preserved attributes above, so this operation always saves the complete final requirement.
                var product = await _productService.UpdateForRequirementAsync(postId, requirement, cancellationToken);
                if (!product.IsSuccess) return Result<PostResponse>.Fail(product.Error!);
                if (material || previousQuantity != current.Quantity || current.Status == PostStatus.Closed)
                    await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);

                var buyPostAuditDiff = new AuditDiffBuilder()
                    .Add("minExpectedPrice", previousMinExpectedPrice, current.MinExpectedPrice)
                    .Add("basePrice", previousBasePrice, current.BasePrice)
                    .Add("quantity", previousQuantity, current.Quantity)
                    .Add("status", previousPostStatus.ToString(), current.Status.ToString())
                    .Add("expiryDate", previousExpiryDate, current.ExpiryDate);

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.PostUpdate,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = ownerId,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = current.PostId,
                    OldValues = buyPostAuditDiff.OldValues,
                    NewValues = buyPostAuditDiff.NewValues
                }, cancellationToken);

                if (agreed < previousQuantity && agreed >= current.Quantity)
                {
                    var notification = await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(ownerId, "Tin thu mua đã đạt mục tiêu",
                            $"Tin thu mua đã lập hợp đồng đủ {agreed}/{current.Quantity} sản phẩm và được đóng. Vui lòng kiểm tra và chỉnh sửa số lượng nếu muốn tiếp tục thu mua.",
                            NotificationTargetType.Post, postId), cancellationToken);
                    _unitOfWork.RegisterAfterCommit(() => _notificationService.PublishCreatedSafelyAsync(notification));
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<PostResponse>.Success(_mapper.Map<PostResponse>(await _postRepository.GetByIdAsync(postId, cancellationToken)));
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }

        public Task<Result<bool>> DeleteBuyPostAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default) =>
            ChangeLifecycleAsync(ownerId, postId, PostStatus.Deleted, true, cancellationToken);

        private async Task<Result<bool>> DeleteBuyPostAsAdminAsync(Guid postId, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var post = await _postRepository.GetByIdForUpdateAsync(postId, cancellationToken);
                if (post is null || post.Status == PostStatus.Deleted)
                    return Result<bool>.Fail(PostErrors.NotFound);

                if (await _postRepository.HasUnfinishedTransactionsAsync(postId, cancellationToken))
                    return Result<bool>.Fail(ValidationErrors.InvalidRequest("Bài đăng đang có giao dịch chưa hoàn tất."));
                var previousPostStatus = post.Status;
                post.Status = PostStatus.Deleted;
                post.UpdatedAt = DateTime.UtcNow;
                await _postRepository.UpdateAsync(post, cancellationToken);
                await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, cancellationToken);

                var deleteBuyPostAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousPostStatus.ToString(), post.Status.ToString());

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.Administration,
                    Action = AuditActions.PostDelete,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = post.PostId,
                    OldValues = deleteBuyPostAuditDiff.OldValues,
                    NewValues = deleteBuyPostAuditDiff.NewValues
                }, cancellationToken);


                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        private async Task<Result<bool>> ChangeLifecycleAsync(Guid ownerId, Guid postId, PostStatus status, bool buyOnly, CancellationToken ct)
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var p = await _postRepository.GetByIdForUpdateAsync(postId, ct);
                if (p == null) return Result<bool>.Fail(PostErrors.NotFound);
                if (p.OwnerId != ownerId) return Result<bool>.Fail(PostErrors.Forbidden);
                if (buyOnly && p.PostType != PostType.Buy) return Result<bool>.Fail(PostErrors.InvalidPostType);
                if (p.Status is PostStatus.Deleted or PostStatus.Suspended) return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
                if (status is PostStatus.Active or PostStatus.Deleted && p.ExpiryDate <= DateTime.UtcNow) return Result<bool>.Fail(PostErrors.PostExpired);
                if (status == PostStatus.Active && (p.Status != PostStatus.Closed || p.RemainingQuantity <= 0)) return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);
                if (status == PostStatus.Active && p.PostType == PostType.Buy &&
                    await _postRepository.GetAgreedBuyQuantityAsync(postId, null, ct) >= p.Quantity)
                    return Result<bool>.Fail(ValidationErrors.InvalidRequest("Tin đã đạt mục tiêu thu mua. Vui lòng kiểm tra và tăng số lượng trước khi mở lại."));
                if (status == PostStatus.Deleted && await _postRepository.HasUnfinishedTransactionsAsync(postId, ct))
                    return Result<bool>.Fail(ValidationErrors.InvalidRequest("Bài đăng đang có giao dịch chưa hoàn tất. Bạn có thể đóng nhận đề nghị mới."));
                var previousPostStatus = p.Status;
                p.Status = status; p.UpdatedAt = DateTime.UtcNow;
                await _postRepository.UpdateAsync(p, ct);
                if (status != PostStatus.Active) await _offerRepository.ClosePendingByPostAsync(postId, OfferStatus.Closed, ct);

                var lifecycleAuditAction = status switch
                {
                    PostStatus.Closed => AuditActions.PostClose,
                    PostStatus.Active => AuditActions.PostReactivate,
                    PostStatus.Deleted => AuditActions.PostDelete,
                    _ => throw new InvalidOperationException($"Unsupported audited post status: {status}.")
                };

                var postLifecycleAuditDiff = new AuditDiffBuilder()
                    .Add("status", previousPostStatus.ToString(), p.Status.ToString());

                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = lifecycleAuditAction,
                    Outcome = AuditOutcome.Success,
                    ActorType = AuditActorType.User,
                    UserId = ownerId,
                    TargetType = AuditTargetTypes.Post,
                    TargetId = p.PostId,
                    OldValues = postLifecycleAuditDiff.OldValues,
                    NewValues = postLifecycleAuditDiff.NewValues
                }, ct);

                await _unitOfWork.SaveChangesAsync(ct); await _unitOfWork.CommitTransactionAsync(ct);
                return Result<bool>.Success(true);
            }
            finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
        }

        public async Task ExpireBuyPostsAsync(CancellationToken cancellationToken = default)
        {
            var ids = await _postRepository.GetExpiredBuyPostIdsAsync(DateTime.UtcNow, 100, cancellationToken);
            foreach (var id in ids)
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    var p = await _postRepository.GetByIdForUpdateAsync(id, cancellationToken);
                    if (p?.Status == PostStatus.Active && p.ExpiryDate <= DateTime.UtcNow)
                    {
                        p.Status = PostStatus.Closed; p.UpdatedAt = DateTime.UtcNow;
                        await _postRepository.UpdateAsync(p, cancellationToken);
                        await _offerRepository.ClosePendingByPostAsync(id, OfferStatus.Expired, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                finally { await _unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
            }
        }

        public async Task<Result<PagedResult<BuyPostMatchResponse>>> GetMatchesAsync(Guid ownerId, Guid buyPostId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
            if (buy == null || buy.Product == null || buy.User?.Status != UserStatus.Active || buy.PostType != PostType.Buy || !TradingPostRules.IsAvailable(buy))
                return Result<PagedResult<BuyPostMatchResponse>>.Fail(PostErrors.NotFound);
            if (buy.OwnerId != ownerId)
                return Result<PagedResult<BuyPostMatchResponse>>.Fail(PostErrors.Forbidden);
            var matched = await _supplierMatchService.MatchBackendOnlyAsync(
                SupplierDemandContextBuilder.FromBuyPost(buy),
                (request.PageNumber - 1) * request.PageSize,
                request.PageSize,
                cancellationToken);
            return Result<PagedResult<BuyPostMatchResponse>>.Success(new PagedResult<BuyPostMatchResponse> {
                Items = matched.Matches,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = matched.CandidateCount
            });
        }

        public async Task<Result<SupplierMatchResponse>> GetSupplierMatchesAsync(
            Guid ownerId,
            Guid buyPostId,
            SupplierMatchAdvancedFilterRequest? advancedFilters,
            CancellationToken cancellationToken = default)
        {
            var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
            if (buy == null || buy.Product == null || buy.User?.Status != UserStatus.Active ||
                buy.PostType != PostType.Buy || !TradingPostRules.IsAvailable(buy))
                return Result<SupplierMatchResponse>.Fail(PostErrors.NotFound);
            if (buy.OwnerId != ownerId)
                return Result<SupplierMatchResponse>.Fail(PostErrors.Forbidden);

            var response = await _supplierMatchService.MatchAsync(
                SupplierDemandContextBuilder.FromBuyPost(buy, advancedFilters), 0, int.MaxValue, cancellationToken);
            response.BuyPostId = buyPostId;
            return Result<SupplierMatchResponse>.Success(response);
        }

        private async Task TryWarmSupplierMatchesAsync(Guid ownerId, Guid buyPostId, CancellationToken cancellationToken)
        {
            try
            {
                var buy = await _postRepository.GetDetailByIdAsync(buyPostId, cancellationToken);
                if (buy?.Product is null || buy.OwnerId != ownerId || buy.PostType != PostType.Buy)
                    return;
                await _supplierMatchService.MatchInitialBackendAsync(
                    SupplierDemandContextBuilder.FromBuyPost(buy), 3, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception,
                    "Không thể tạo supplier matches ban đầu cho BuyPostId {BuyPostId}", buyPostId);
            }
        }
    }
}
