using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Categories;
using HomeCycle.Application.DTOs.Responses.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface ICategoryService
    {
        Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

        Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<CategoryResponse>>> GetActiveAsync(CategorySearchRequest request, CancellationToken cancellationToken = default);

        Task<Result<CategoryResponse>> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<CategoryResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<CategoryResponse>>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken = default);
    }
}
