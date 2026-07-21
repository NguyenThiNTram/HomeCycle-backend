using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Application.DTOs.Responses.Brands;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IBrandService
    {
        Task<Result<BrandResponse>> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default);

        Task<Result<BrandResponse>> UpdateBrandAsync(Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> DeleteBrandAsync(Guid brandId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<BrandResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<BrandResponse>>> SearchAsync(BrandSearchRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<brand>> GetActiveAsync(BrandSearchRequest request, CancellationToken cancellationToken = default);

        Task<Result<BrandResponse>> GetByIdAsync(Guid brandId, CancellationToken cancellationToken = default);
    }
}
