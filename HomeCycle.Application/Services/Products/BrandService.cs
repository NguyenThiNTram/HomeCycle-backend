using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Application.DTOs.Responses.Brands;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using MathNet.Numerics.Statistics.Mcmc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Products
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private readonly IValidator<CreateBrandRequest> _createValidator;
        private readonly IValidator<UpdateBrandRequest> _updateValidator;
        private readonly ILogger<BrandService> _logger;

        public BrandService(
            IBrandRepository brandRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateBrandRequest> createValidator,
            IValidator<UpdateBrandRequest> updateValidator,
            ILogger<BrandService> logger)
        {
            _brandRepository = brandRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        public async Task<Result<BrandResponse>> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
                return Result<BrandResponse>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(
                            ", ",
                            validation.Errors.Select(x => x.ErrorMessage))));

            if (await _brandRepository.ExistsByNameAsync(request.BrandName, cancellationToken))
                return Result<BrandResponse>.Fail(BrandErrors.BrandAlreadyExists);

            var entity = _mapper.Map<brand>(request);

            entity.BrandId = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;

            await _brandRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<BrandResponse>.Success(
                _mapper.Map<BrandResponse>(entity));
        }

        public async Task<Result<BrandResponse>> UpdateBrandAsync(Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<BrandResponse>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(
                            ", ",
                            validation.Errors.Select(x => x.ErrorMessage))));

            var brand = await _brandRepository.GetByIdAsync(brandId, cancellationToken);
            if (brand == null)
                return Result<BrandResponse>.Fail(BrandErrors.BrandNotFound);

            var exists = await _brandRepository.ExistsByNameAsync(request.BrandName, cancellationToken);
            if (exists && !brand.BrandName!.Equals(request.BrandName.Trim(), StringComparison.OrdinalIgnoreCase))
                return Result<BrandResponse>.Fail(ValidationErrors.InvalidRequest("Brand name already exists."));

            if (!brand.BrandName.Equals(request.BrandName.Trim(),StringComparison.OrdinalIgnoreCase))
            {
                if (await _brandRepository.ExistsByNameAsync(
                    request.BrandName,
                    cancellationToken))
                {
                    return Result<BrandResponse>.Fail(
                        BrandErrors.BrandAlreadyExists);
                }
            }

            _mapper.Map(request,brand);

            await _brandRepository.UpdateAsync(brand, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Brand {BrandId} updated.", brandId);

            return Result<BrandResponse>.Success( _mapper.Map<BrandResponse>(brand));
        }

        public async Task<Result<bool>> DeleteBrandAsync(Guid brandId, CancellationToken cancellationToken = default)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId, cancellationToken);

            if (brand == null)
                return Result<bool>.Fail(BrandErrors.BrandNotFound);

            brand.IsActive = false;

            await _brandRepository.UpdateAsync(brand, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<BrandResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _brandRepository.GetAllAsync(request, cancellationToken);

            return Result<PagedResult<BrandResponse>>.Success(
                new PagedResult<BrandResponse>
                {
                    Items = _mapper.Map<IReadOnlyList<BrandResponse>>(
                        result.Items),

                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages
                });
        }

        public async Task<Result<BrandResponse>> GetByIdAsync(Guid brandId, CancellationToken cancellationToken = default)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId, cancellationToken);

            if (brand == null)
                return Result<BrandResponse>.Fail(
                    BrandErrors.BrandNotFound);

            return Result<BrandResponse>.Success(
                _mapper.Map<BrandResponse>(brand));
        }

        public async Task<Result<PagedResult<BrandResponse>>> SearchAsync(BrandSearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _brandRepository.SearchAsync(request.Keyword ?? string.Empty, request, cancellationToken);

            return Result<PagedResult<BrandResponse>>.Success(
                new PagedResult<BrandResponse>
                {
                    Items = _mapper.Map<IReadOnlyList<BrandResponse>>(result.Items),
                    PageNumber = result.PageNumber,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages
                }
            );
        }

        public async Task<PagedResult<brand>> GetActiveAsync(BrandSearchRequest request, CancellationToken cancellationToken = default)
        {
            var pagination = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return await _brandRepository.GetActiveAsync(pagination, cancellationToken);
        }

    }
}
