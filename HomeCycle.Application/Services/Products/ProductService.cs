using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Application.Validations.Products;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductAttributeValueRepository _attributeValueRepository;
        private readonly IProductTypeRepository _productTypeRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IValidator<ProductRequest> _productRequestValidator;
        private readonly IValidator<ProductRequirementRequest> _productRequirementValidator;
        private readonly IValidator<ProductAttributeValueRequest> _attributeValueValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(
            IProductRepository productRepository,
            IProductAttributeValueRepository attributeValueRepository,
            IProductTypeRepository productTypeRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IValidator<ProductRequest> productRequestValidator,
            IValidator<ProductRequirementRequest> productRequirementValidator,
            IValidator<ProductAttributeValueRequest> attributeValueValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _attributeValueRepository = attributeValueRepository;
            _productTypeRepository = productTypeRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productRequestValidator = productRequestValidator;
            _productRequirementValidator = productRequirementValidator;
            _attributeValueValidator = attributeValueValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        //Sell
        public async Task<Result<product>> PrepareForCreateAsync(Guid postId, ProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _productRequestValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var referenceError = await ValidateReferencesAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            if (request.AttributeValues != null && request.AttributeValues.Any())
            {
                foreach (var attrReq in request.AttributeValues)
                {
                    var attrValidation = await _attributeValueValidator.ValidateAsync(attrReq, cancellationToken);
                    if (!attrValidation.IsValid)
                    {
                        return Result<product>.Fail(
                            ValidationErrors.InvalidRequest(
                                string.Join(", ", attrValidation.Errors.Select(e => e.ErrorMessage))));
                    }
                }
            }

            //var productId = Guid.NewGuid();

            //var entity = new product
            //{
            //    ProductId = productId,
            //    PostId = postId,
            //    CategoryId = request.CategoryId,
            //    ProductTypeId = request.ProductTypeId,
            //    BrandId = request.BrandId,
            //    ProductName = request.ProductName,
            //    SpaceUsage = request.SpaceUsage,
            //    ModelNumber = request.ModelNumber,
            //    OriginalPrice = request.OriginalPrice,
            //    Length = request.Length,
            //    Width = request.Width,
            //    Height = request.Height,
            //    Weight = request.Weight,
            //    FunctionalityStatus = request.FunctionalityStatus,
            //    UsageDuration = request.UsageDuration,
            //    DamageLevel = request.DamageLevel,
            //    DetailDescription = request.DetailDescription
            //};

            var entity = _mapper.Map<product>(request);

            entity.ProductId = Guid.NewGuid();
            entity.PostId = postId;

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _productRepository.AddAsync(entity, cancellationToken);

                //if (request.AttributeValues.Any())
                //{
                //    var values = request.AttributeValues.Select(x =>
                //        new product_attribute_value
                //        {
                //            ProductId = entity.ProductId,
                //            AttributeId = x.AttributeId,
                //            OptionId = x.OptionId,

                //            InputType = x.InputType,

                //            ValueBoolean = x.ValueBoolean,
                //            ValueText = x.ValueText,
                //            ValueNumber = x.ValueNumber
                //        });

                //    await _attributeValueRepository
                //        .AddRangeAsync(values, cancellationToken);
                //}

                await SaveAttributeValuesAsync(entity.ProductId, request.AttributeValues.ToList(), cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<product>.Success(entity);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Result<product>> PrepareForUpdateAsync(Guid postId, ProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _productRequestValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var existing = await _productRepository.GetByPostIdAsync(postId, cancellationToken);
            if (existing is null)
            {
                return Result<product>.Fail(ProductErrors.ProductNotFound);
            }

            var referenceError = await ValidateReferencesAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            if (request.AttributeValues != null && request.AttributeValues.Any())
            {
                foreach (var attrReq in request.AttributeValues)
                {
                    var attrValidation = await _attributeValueValidator.ValidateAsync(attrReq, cancellationToken);
                    if (!attrValidation.IsValid)
                    {
                        return Result<product>.Fail(
                            ValidationErrors.InvalidRequest(
                                string.Join(", ", attrValidation.Errors.Select(e => e.ErrorMessage))));
                    }
                }
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                //existing.CategoryId = request.CategoryId;
                //existing.ProductTypeId = request.ProductTypeId;
                //existing.BrandId = request.BrandId;
                //existing.ProductName = request.ProductName;
                //existing.SpaceUsage = request.SpaceUsage;
                //existing.ModelNumber = request.ModelNumber;
                //existing.OriginalPrice = request.OriginalPrice;
                //existing.Length = request.Length;
                //existing.Width = request.Width;
                //existing.Height = request.Height;
                //existing.Weight = request.Weight;
                //existing.FunctionalityStatus = request.FunctionalityStatus;
                //existing.UsageDuration = request.UsageDuration;
                //existing.DamageLevel = request.DamageLevel;
                //existing.DetailDescription = request.DetailDescription;

                _mapper.Map(request, existing);

                await _productRepository.UpdateAsync(existing, cancellationToken);
                await _attributeValueRepository.RemoveByProductIdAsync(existing.ProductId, cancellationToken);

                if (request.AttributeValues.Any())
                {
                    var values = request.AttributeValues.Select(x =>
                        new product_attribute_value
                        {
                            ProductId = existing.ProductId,
                            AttributeId = x.AttributeId,
                            OptionId = x.OptionId,
                            InputType = x.InputType,
                            ValueBoolean = x.ValueBoolean,
                            ValueText = x.ValueText,
                            ValueNumber = x.ValueNumber
                        });

                    await _attributeValueRepository.AddRangeAsync(values, cancellationToken);
                }

                await SaveAttributeValuesAsync(existing.ProductId, request.AttributeValues.ToList(), cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<product>.Success(existing);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        //Buy
        public async Task<Result<product>> PrepareForRequirementAsync(Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _productRequirementValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var referenceError = await ValidateReferencesAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            var attributeError = await ValidateAttributeValuesAsync(request.AttributeValues, cancellationToken);
            if (attributeError is not null)
                return Result<product>.Fail(attributeError);

            var entity = _mapper.Map<product>(request);

            entity.ProductId = Guid.NewGuid();
            entity.PostId = postId;

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _productRepository.AddAsync(entity, cancellationToken);

                await SaveAttributeValuesAsync(entity.ProductId, request.AttributeValues, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<product>.Success(entity);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Result<product>> UpdateForRequirementAsync(Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _productRequirementValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join(", ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var existing = await _productRepository.GetByPostIdAsync(postId, cancellationToken);

            if (existing is null)
                return Result<product>.Fail(ProductErrors.ProductNotFound);

            var referenceError = await ValidateReferencesAsync(
                 request.CategoryId, request.ProductTypeId, request.BrandId, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            var attributeError = await ValidateAttributeValuesAsync(request.AttributeValues, cancellationToken);
            if (attributeError is not null)
                return Result<product>.Fail(attributeError);

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                _mapper.Map(request, existing);

                await _productRepository.UpdateAsync(existing, cancellationToken);
                await _attributeValueRepository.RemoveByProductIdAsync(existing.ProductId, cancellationToken);
                await SaveAttributeValuesAsync(existing.ProductId, request.AttributeValues, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                return Result<product>.Success(existing);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Result<ProductResponse>> GetDetailByPostIdAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _productRepository.GetDetailByPostIdAsync(postId, cancellationToken);
            if (entity is null)
                return Result<ProductResponse>.Fail(ProductErrors.ProductNotFound);

            var response = _mapper.Map<ProductResponse>(entity);
            return Result<ProductResponse>.Success(response);
        }

        public async Task<Result<ProductResponse>> GetDetailAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _productRepository.GetDetailAsync(productId, cancellationToken);

            if (entity is null)
                return Result<ProductResponse>.Fail(ProductErrors.ProductNotFound);

            var response = _mapper.Map<ProductResponse>(entity);

            return Result<ProductResponse>.Success(response);
        }

        private async Task<Error?> ValidateReferencesAsync(
            Guid categoryId,
            Guid productTypeId,
            Guid? brandId,
            CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
                return ProductErrors.InvalidCategory;

            var productType = await _productTypeRepository.GetByIdAsync(productTypeId, cancellationToken);
            if (productType is null || productType.CategoryId != categoryId)
                return ProductErrors.InvalidProductType;

            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value, cancellationToken);
                if (brand is null)
                    return ProductErrors.InvalidBrand;
            }

            return null;
        }

        private async Task<Error?> ValidateAttributeValuesAsync(
            List<ProductAttributeValueRequest>? attributeValues,
            CancellationToken cancellationToken)
        {
            if (attributeValues is null || !attributeValues.Any())
                return null;

            foreach (var attrReq in attributeValues)
            {
                var attrValidation = await _attributeValueValidator.ValidateAsync(attrReq, cancellationToken);
                if (!attrValidation.IsValid)
                {
                    return ValidationErrors.InvalidRequest(
                        string.Join(", ", attrValidation.Errors.Select(e => e.ErrorMessage)));
                }
            }

            return null;
        }

        /// <summary>
        /// Lưu tập thuộc tính động của sản phẩm
        /// </summary>
        private async Task SaveAttributeValuesAsync(
            Guid productId,
            List<ProductAttributeValueRequest>? attributeValues,
            CancellationToken cancellationToken)
        {
            if (attributeValues is null || !attributeValues.Any())
                return;

            var values = attributeValues.Select(x => new product_attribute_value
            {
                ProductId = productId,
                AttributeId = x.AttributeId,
                OptionId = x.OptionId,
                InputType = x.InputType,
                ValueBoolean = x.ValueBoolean,
                ValueText = x.ValueText,
                ValueNumber = x.ValueNumber
            });

            await _attributeValueRepository.AddRangeAsync(values, cancellationToken);
        }
    }
}
