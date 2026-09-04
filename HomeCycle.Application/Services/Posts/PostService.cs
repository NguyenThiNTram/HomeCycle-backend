using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Users;
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Posts
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
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

        private const string PostMediaTargetType = "Post";
        private const string PostMediaFolder = "posts";

        public PostService(
            IPostRepository postRepository,
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
            IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
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
        }

        // ================== CREATE - SELL ==================

        public async Task<Result<PostResponse>> CreateSellPostAsync(
            Guid ownerId, CreateSellPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createSellValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var roleError = await ValidateCreateRoleAsync(ownerId, UserRole.Personal, cancellationToken);
            if (roleError is not null)
                return Result<PostResponse>.Fail(roleError);

            if (request.Medias == null || !request.Medias.Any())
            {
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest("Bài đăng bán bắt buộc phải có ít nhất 1 hình ảnh sản phẩm."));
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
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                    targetId: post.PostId,
                    targetType: PostMediaTargetType,
                    folderName: PostMediaFolder,
                    files: request.Medias,
                    cancellationToken: cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(mediaResult.Error!);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(post);
                return Result<PostResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ================== CREATE - BUY ==================

        public async Task<Result<PostResponse>> CreateBuyPostAsync(
            Guid ownerId, CreateBuyPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createBuyValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var roleError = await ValidateCreateRoleAsync(ownerId, UserRole.Business, cancellationToken);
            if (roleError is not null)
                return Result<PostResponse>.Fail(roleError);

            var now = DateTime.UtcNow;
            var post = _mapper.Map<post>(request);

            post.PostId = Guid.NewGuid();
            post.OwnerId = ownerId;
            post.PostType = PostType.Buy;
            //post.ProductName = request.Requirement?.ProductName;
            post.BasePrice = request.ExpectedPrice;
            post.CreatedAt = now;
            post.UpdatedAt = now;
            post.RemainingQuantity = request.Quantity;
            post.Status = PostStatus.Active;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _postRepository.AddAsync(post, cancellationToken);

                var productResult = await _productService.PrepareForRequirementAsync(post.PostId, request.Requirement, cancellationToken);
                if (!productResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                var mediaResult = await _mediaService.UploadAndSaveMediaAsync(
                    targetId: post.PostId,
                    targetType: PostMediaTargetType,
                    folderName: PostMediaFolder,
                    files: request.Medias,
                    cancellationToken: cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(mediaResult.Error!);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(post);
                return Result<PostResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
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

            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);

            var checkError = ValidateOwnershipAndComputeRemaining(
                existing, ownerId, PostType.Sell, request.Quantity ?? existing.Quantity, out int newRemainingQuantity);
            if (checkError is not null)
                return Result<PostResponse>.Fail(checkError);

            _mapper.Map(request, existing);
            existing!.BasePrice = request.BasePrice;
            //existing.ProductName = request.Product?.ProductName;
            existing.RemainingQuantity = newRemainingQuantity;
            existing.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
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
                    var mediaResult = await _mediaService.ReplaceMediaAsync(
                        postId, PostMediaTargetType, PostMediaFolder, request.Medias, cancellationToken);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PostResponse>.Fail(mediaResult.Error!);
                    }
                }
                // Nếu không có Medias trong request -> Bỏ qua, giữ nguyên ảnh hiện tại trong DB.

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(existing);
                return Result<PostResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        // ================== UPDATE - BUY ==================

        public async Task<Result<PostResponse>> UpdateBuyPostAsync(
            Guid ownerId, Guid postId, UpdateBuyPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateBuyValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);

            var checkError = ValidateOwnershipAndComputeRemaining(
                existing, ownerId, PostType.Buy, (int)request.Quantity, out int newRemainingQuantity);
            if (checkError is not null)
                return Result<PostResponse>.Fail(checkError);

            _mapper.Map(request, existing);
            existing!.BasePrice = request.ExpectedPrice;
            //existing.ProductName = request.Requirement?.ProductName;
            existing.RemainingQuantity = newRemainingQuantity;
            existing.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _postRepository.UpdateAsync(existing, cancellationToken);

                var productResult = await _productService.UpdateForRequirementAsync(postId, request.Requirement, cancellationToken);
                if (!productResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                // Kiểm tra xem request có chứa danh sách ảnh mới không
                if (request.Medias != null && request.Medias.Any())
                {
                    var mediaResult = await _mediaService.ReplaceMediaAsync(
                        postId, PostMediaTargetType, PostMediaFolder, request.Medias, cancellationToken);

                    if (!mediaResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PostResponse>.Fail(mediaResult.Error!);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<PostResponse>(existing);
                return Result<PostResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
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

            var (averageRating, totalReviews) = await _reviewRepository.GetRatingStatsByRevieweeIdAsync(entity.OwnerId, cancellationToken);

            response.AverageRating = averageRating;
            response.TotalReviews = totalReviews;

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

        public async Task<Result<PagedResult<PostResponse>>> SearchAsync(
            PostSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PagedResult<PostResponse>>.Fail(
                    ValidationErrors.InvalidRequest(string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));

            var paged = await _postRepository.SearchAsync(request, cancellationToken);

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

        public async Task<Result<bool>> CloseAsync(
            Guid ownerId,
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (existing is null)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (existing.OwnerId != ownerId)
                return Result<bool>.Fail(PostErrors.Forbidden);

            if (existing.Status == PostStatus.Closed)
                return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);

            var updated = await _postRepository.UpdateStatusAsync(postId, PostStatus.Closed, cancellationToken);
            if (!updated)
                return Result<bool>.Fail(PostErrors.NotFound);

            existing.Status = PostStatus.Closed;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ReactivateAsync(
            Guid ownerId,
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (existing is null)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (existing.OwnerId != ownerId)
                return Result<bool>.Fail(PostErrors.Forbidden);

            if (existing.Status != PostStatus.Closed)
                return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);

            var updated = await _postRepository.UpdateStatusAsync(postId, PostStatus.Active, cancellationToken);
            if (!updated)
                return Result<bool>.Fail(PostErrors.NotFound);

            existing.Status = PostStatus.Active;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid ownerId,
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (existing is null)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (existing.OwnerId != ownerId)
                return Result<bool>.Fail(PostErrors.Forbidden);

            var deleted = await _postRepository.DeleteAsync(postId, cancellationToken);
            if (!deleted)
                return Result<bool>.Fail(PostErrors.NotFound);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        // ================== MODERATOR - SUSPEND ==================

        public async Task<Result<bool>> SuspendAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (existing is null || existing.Status == PostStatus.Deleted)
                return Result<bool>.Fail(PostErrors.NotFound);

            if (existing.Status == PostStatus.Suspended)
                return Result<bool>.Fail(PostErrors.PostAlreadySuspended);

            var updated = await _postRepository.UpdateStatusAsync(postId, PostStatus.Suspended, cancellationToken);
            if (!updated)
                return Result<bool>.Fail(PostErrors.NotFound);

            existing.Status = PostStatus.Suspended;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        private async Task<Error?> ValidateCreateRoleAsync(
            Guid ownerId, UserRole requiredRole, CancellationToken cancellationToken)
        {
            var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
            if (owner is null)
                return PostErrors.RoleNotAllowed;

            return owner.Role == requiredRole
                ? null
                : PostErrors.RoleNotAllowed;
        }

        private Error? ValidateOwnershipAndComputeRemaining(
            post? existing, Guid ownerId, PostType postType, int newQuantity, out int newRemainingQuantity)
        {
            newRemainingQuantity = 0;

            if (existing is null)
                return PostErrors.NotFound;

            if (existing.OwnerId != ownerId)
                return PostErrors.Forbidden;

            if (existing.PostType != postType)
                return PostErrors.InvalidPostType;

            if (existing.Status == PostStatus.Deleted || existing.Status == PostStatus.Closed)
                return PostErrors.PostAlreadyClosedOrDeleted;

            // Spec: "Sửa hoặc xóa tin trong thời hạn cho phép"
            if (existing.ExpiryDate.HasValue && existing.ExpiryDate.Value < DateTime.UtcNow)
                return PostErrors.PostExpired;

            int soldQuantity = existing.Quantity - existing.RemainingQuantity;
            newRemainingQuantity = newQuantity - soldQuantity;

            if (newRemainingQuantity < 0)
                return PostErrors.InvalidUpdateQuantity(soldQuantity, newQuantity);

            return null;
        }

    }
}
