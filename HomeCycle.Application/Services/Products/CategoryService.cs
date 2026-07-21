using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Categories;
using HomeCycle.Application.DTOs.Responses.Categories;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Products
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;
        private readonly IValidator<CreateCategoryRequest> _createCategoryValidator;
        private readonly IValidator<UpdateCategoryRequest> _updateCategoryValidator;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CategoryService> logger,
            IValidator<CreateCategoryRequest> createCategoryValidator,
            IValidator<UpdateCategoryRequest> updateCategoryValidator)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _createCategoryValidator = createCategoryValidator;
            _updateCategoryValidator = updateCategoryValidator;
        }

        public async Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createCategoryValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<CategoryResponse>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            if (await _categoryRepository.ExistsByNameAsync(request.CategoryName, cancellationToken))
            {
                return Result<CategoryResponse>.Fail(CategoryErrors.CategoryAlreadyExists);
            }

            var entity = _mapper.Map<category>(request);

            entity.CategoryId = Guid.NewGuid();
            //entity.CategoryName = entity.CategoryName.Trim();
            //entity.Description = entity.Description?.Trim();
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;

            await _categoryRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<CategoryResponse>(entity);
            return Result<CategoryResponse>.Success(dto);
        }

        public async Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateCategoryValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<CategoryResponse>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<CategoryResponse>.Fail(CategoryErrors.CategoryNotFound);
            }

            if (!category.CategoryName!.Equals(request.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (await _categoryRepository.ExistsByNameAsync(request.CategoryName, cancellationToken))
                {
                    return Result<CategoryResponse>.Fail(CategoryErrors.CategoryAlreadyExists);
                }
            }

            //category.CategoryName = request.CategoryName.Trim();
            //category.Description = request.Description?.Trim();
            //category.IsActive = request.IsActive;
            _mapper.Map(request, category);

            await _categoryRepository.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} updated.", categoryId);

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
        }

        public async Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<bool>.Fail(CategoryErrors.CategoryNotFound);
            }

            category.IsActive = false;

            await _categoryRepository.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} deactivated.", categoryId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<CategoryResponse>>> GetActiveAsync(CategorySearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _categoryRepository.GetActiveAsync(request, cancellationToken);

            return Result<PagedResult<CategoryResponse>>.Success(
                new PagedResult<CategoryResponse>
                {
                    Items = _mapper.Map<IReadOnlyList<CategoryResponse>>(result.Items),
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages
                });
        }

        public async Task<Result<CategoryResponse>> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

            if (category == null)
            {
                return Result<CategoryResponse>.Fail(CategoryErrors.CategoryNotFound);
            }

            return Result<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
        }

        public async Task<Result<PagedResult<CategoryResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            //var pagination = new PaginationRequest
            //{
            //    PageNumber = request.PageNumber,
            //    PageSize = request.PageSize
            //};

            //var result = await _categoryRepository.GetAllAsync(pagination, cancellationToken);
            var result = await _categoryRepository.GetAllAsync(request, cancellationToken);

            return Result<PagedResult<CategoryResponse>>.Success(
                new PagedResult<CategoryResponse>
                {
                    Items = _mapper.Map<IReadOnlyList<CategoryResponse>>(result.Items),
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages
                });
        }

        public async Task<Result<PagedResult<CategoryResponse>>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _categoryRepository.SearchAsync(
                request.Keyword ?? string.Empty,
                request,
                cancellationToken);

            return Result<PagedResult<CategoryResponse>>.Success(
                new PagedResult<CategoryResponse>
                {
                    Items = _mapper.Map<IReadOnlyList<CategoryResponse>>(result.Items),
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages
                });
        }

    }
}
