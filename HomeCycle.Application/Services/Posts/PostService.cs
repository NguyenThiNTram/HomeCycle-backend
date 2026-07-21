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
using HomeCycle.Application.Interfaces.Services.Posts;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Posts
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
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
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            if (request.Medias == null || !request.Medias.Any())
            {
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest("Bài đăng bán bắt buộc phải có ít nhất 1 hình ảnh sản phẩm."));
            }

            var now = DateTime.UtcNow;
            var post = _mapper.Map<post>(request);

            post.PostId = Guid.NewGuid();
            post.OwnerId = ownerId;
            post.PostType = (int)PostType.Sell;
            post.BasePrice = request.BasePrice;
            post.CreatedAt = now;
            post.UpdatedAt = now;
            post.RemainingQuantity = request.Quantity;
            post.Status = (int)PostStatus.Pending;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _postRepository.AddAsync(post, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Post phải tồn tại trước do FK Product.PostId

                var productResult = await _productService.PrepareForCreateAsync(post.PostId, request.Product, cancellationToken);
                if (!productResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(productResult.Error!);
                }

                //if (request.Medias != null && request.Medias.Count > 0)
                //{
                //    var mediaResult = await _mediaService.UploadRawFilesAsync(
                //        new DirectUploadMediaRequest
                //        {
                //            TargetId = post.PostId,
                //            TargetType = PostMediaTargetType,
                //            TargetFolder = PostMediaFolder,
                //            Medias = (List<MediaRequest>)request.Medias
                //        });

                //    if (!mediaResult.IsSuccess)
                //    {
                //        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                //        return Result<PostResponse>.Fail(mediaResult.Error!);
                //    }
                //}

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
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var now = DateTime.UtcNow;
            var post = _mapper.Map<post>(request);

            post.PostId = Guid.NewGuid();
            post.OwnerId = ownerId;
            post.PostType = PostType.Buy;
            post.BasePrice = request.ExpectedPrice;
            post.CreatedAt = now;
            post.UpdatedAt = now;
            post.RemainingQuantity = request.Quantity;
            post.Status = (int)PostStatus.Pending;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _postRepository.AddAsync(post, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);

            var checkError = ValidateOwnershipAndComputeRemaining(
                existing, ownerId, request.Quantity, out int newRemainingQuantity);
            if (checkError is not null)
                return Result<PostResponse>.Fail(checkError);

            _mapper.Map(request, existing);
            existing!.BasePrice = request.BasePrice;
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

                var mediaResult = await _mediaService.ReplaceMediaAsync(
                    postId, PostMediaTargetType, PostMediaFolder, request.Medias, cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(mediaResult.Error!);
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

        // ================== UPDATE - BUY ==================

        public async Task<Result<PostResponse>> UpdateBuyPostAsync(
            Guid ownerId, Guid postId, UpdateBuyPostRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateBuyValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PostResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var existing = await _postRepository.GetByIdAsync(postId, cancellationToken);

            var checkError = ValidateOwnershipAndComputeRemaining(
                existing, ownerId, request.Quantity, out int newRemainingQuantity);
            if (checkError is not null)
                return Result<PostResponse>.Fail(checkError);

            _mapper.Map(request, existing);
            existing!.BasePrice = request.ExpectedPrice;
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

                var mediaResult = await _mediaService.ReplaceMediaAsync(
                    postId, PostMediaTargetType, PostMediaFolder, request.Medias, cancellationToken);

                if (!mediaResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<PostResponse>.Fail(mediaResult.Error!);
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

            if (entity is null || entity.Status == (int)PostStatus.Deleted)
                return Result<PostDetailResponse>.Fail(PostErrors.NotFound);

            var response = _mapper.Map<PostDetailResponse>(entity);

            var productTask = _productService.GetDetailByPostIdAsync(postId, cancellationToken);
            var mediaTask = _mediaService.GetByTargetAsync(postId, PostMediaTargetType, cancellationToken);

            await Task.WhenAll(productTask, mediaTask);

            var productResult = await productTask;
            if (productResult.IsSuccess && productResult.Value is ProductResponse productData)
            {
                response.Product = productData;
            }

            var mediaResult = await mediaTask;
            if (mediaResult.IsSuccess && mediaResult.Value is IReadOnlyList<MediaResponse> mediaData)
            {
                response.Medias = mediaData;
            }

            return Result<PostDetailResponse>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> GetAllAsync(
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var paged = await _postRepository.GetAllAsync(request, cancellationToken);

            var response = new PagedResult<PostResponse>
            {
                Items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount
            };

            return Result<PagedResult<PostResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<PostResponse>>> SearchAsync(
            PostSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<PagedResult<PostResponse>>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var paged = await _postRepository.SearchAsync(request, cancellationToken);

            var mappedItems = _mapper.Map<List<PostResponse>>(paged.Items);

            var response = new PagedResult<PostResponse>
            {
                Items = paged.Items.Select(x => _mapper.Map<PostResponse>(x)).ToList(),
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

            if (existing.Status == (int)PostStatus.Closed)
                return Result<bool>.Fail(PostErrors.PostAlreadyClosedOrDeleted);

            var updated = await _postRepository.UpdateStatusAsync(postId, PostStatus.Closed, cancellationToken);
            if (!updated)
                return Result<bool>.Fail(PostErrors.NotFound);

            existing.Status = (int)PostStatus.Closed;
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

            var updated = await _postRepository.UpdateStatusAsync(postId, PostStatus.Deleted, cancellationToken);
            if (!updated)
                return Result<bool>.Fail(PostErrors.NotFound);

            existing.Status = (int)PostStatus.Deleted;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        private Error? ValidateOwnershipAndComputeRemaining(
            post? existing, Guid ownerId, int newQuantity, out int newRemainingQuantity)
        {
            newRemainingQuantity = 0;

            if (existing is null)
                return PostErrors.NotFound;

            if (existing.OwnerId != ownerId)
                return PostErrors.Forbidden;

            if (existing.Status == (int)PostStatus.Deleted || existing.Status == (int)PostStatus.Closed)
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
