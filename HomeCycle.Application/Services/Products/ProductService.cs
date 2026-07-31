using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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
        private readonly IMapper _mapper;

        public ProductService(
            IProductRepository productRepository,
            IProductAttributeValueRepository attributeValueRepository,
            IProductTypeRepository productTypeRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IValidator<ProductRequest> productRequestValidator,
            IValidator<ProductRequirementRequest> productRequirementValidator,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _attributeValueRepository = attributeValueRepository;
            _productTypeRepository = productTypeRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productRequestValidator = productRequestValidator;
            _productRequirementValidator = productRequirementValidator;
            _mapper = mapper;
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
                        string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var referenceError = await ValidateProductDataAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, request.AttributeValues, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            var entity = _mapper.Map<product>(request);

            entity.ProductId = Guid.NewGuid();
            entity.PostId = postId;

            await _productRepository.AddAsync(
                entity,
                cancellationToken);

            await SaveAttributeValuesAsync(
                entity.ProductId,
                request.AttributeValues?.ToList(),
                cancellationToken);

            return Result<product>.Success(entity);
        }

        public async Task<Result<product>> PrepareForUpdateAsync(Guid postId, ProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _productRequestValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join("\n", validation.Errors.Select(e => e.ErrorMessage))));
            }

            var existing = await _productRepository.GetByPostIdAsync(postId, cancellationToken);
            if (existing is null)
            {
                return Result<product>.Fail(ProductErrors.ProductNotFound);
            }

            var referenceError = await ValidateProductDataAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, request.AttributeValues, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            bool isProductTypeChanged = existing.ProductTypeId != request.ProductTypeId;

            _mapper.Map(request, existing);

            await _productRepository.UpdateAsync(existing, cancellationToken);

            await UpdateAttributeValuesAsync(existing.ProductId, request.AttributeValues?.ToList(),
                cancellationToken);

            return Result<product>.Success(existing);
        }

        //Buy
        public async Task<Result<product>> PrepareForRequirementAsync(Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _productRequirementValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var referenceError = await ValidateProductDataAsync(
                request.CategoryId, request.ProductTypeId, request.BrandId, request.AttributeValues, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            var entity = _mapper.Map<product>(request);

            entity.ProductId = Guid.NewGuid();
            entity.PostId = postId;

            await _productRepository.AddAsync(
                entity,
                cancellationToken);

            await SaveAttributeValuesAsync(
                entity.ProductId,
                request.AttributeValues,
                cancellationToken);

            return Result<product>.Success(entity);
        }

        public async Task<Result<product>> UpdateForRequirementAsync(Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _productRequirementValidator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<product>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var existing = await _productRepository.GetByPostIdAsync(postId, cancellationToken);

            if (existing is null)
                return Result<product>.Fail(ProductErrors.ProductNotFound);

            var referenceError = await ValidateProductDataAsync(
                 request.CategoryId, request.ProductTypeId, request.BrandId, request.AttributeValues, cancellationToken);
            if (referenceError is not null)
                return Result<product>.Fail(referenceError);

            _mapper.Map(request, existing);

            await _productRepository.UpdateAsync(
                existing,
                cancellationToken);

            await _attributeValueRepository.RemoveByProductIdAsync(
                existing.ProductId,
                cancellationToken);

            await SaveAttributeValuesAsync(
                existing.ProductId,
                request.AttributeValues,
                cancellationToken);

            return Result<product>.Success(existing);
        }

        public async Task<Result<ProductResponse>> GetDetailByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
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

        private static Error? ValidateAttributeValues(product_type productType, IEnumerable<ProductAttributeValueRequest>? attributeValues)
        {
            var requests = attributeValues?.ToList()
                ?? new List<ProductAttributeValueRequest>();

            var attributes = productType.ProductAttributes?.ToList()
                ?? new List<product_attribute>();

            var attributeById = attributes.ToDictionary(x => x.AttributeId);

            // Tiêu chí 2: Không cho gửi trùng AttributeId
            var duplicatedAttribute = requests
                .GroupBy(x => x.AttributeId)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicatedAttribute is not null)
            {
                return ValidationErrors.InvalidRequest(
                    $"Thuộc tính {duplicatedAttribute.Key} đang được gửi nhiều lần.");
            }

            // Tiêu chí 1: Attribute phải thuộc ProductType
            foreach (var request in requests)
            {
                if (!attributeById.ContainsKey(request.AttributeId))
                {
                    return ValidationErrors.InvalidRequest(
                        $"Thuộc tính {request.AttributeId} không thuộc loại sản phẩm đã chọn.");
                }
            }

            // Tiêu chí 3: Phải gửi đủ thuộc tính bắt buộc
            var providedAttributeIds = requests
                .Select(x => x.AttributeId)
                .ToHashSet();

            var missingRequiredAttributes = attributes
                .Where(x =>
                    x.IsRequired &&
                    !providedAttributeIds.Contains(x.AttributeId))
                .ToList();

            if (missingRequiredAttributes.Count > 0)
            {
                var missingNames = string.Join(
                    ", ",
                    missingRequiredAttributes.Select(x =>
                        x.AttributeName ?? x.AttributeId.ToString()));

                return ValidationErrors.InvalidRequest(
                    $"Thiếu các thuộc tính bắt buộc: {missingNames}.");
            }

            foreach (var request in requests)
            {
                var attribute = attributeById[request.AttributeId];

                var hasOption = request.OptionId.HasValue;

                // ValueText = "" vẫn được xem là có truyền field,
                // sau đó sẽ bị từ chối do không có nội dung hợp lệ.
                var hasText = request.ValueText is not null;
                var hasNumber = request.ValueNumber.HasValue;
                var hasBoolean = request.ValueBoolean.HasValue;

                var customValueCount =
                    (hasText ? 1 : 0) +
                    (hasNumber ? 1 : 0) +
                    (hasBoolean ? 1 : 0);

                // Tiêu chí 4, 5, 6
                var matchesInputMode = attribute.InputMode switch
                {
                    InputMode.OptionOnly =>
                        hasOption && customValueCount == 0,

                    InputMode.CustomOnly =>
                        !hasOption && customValueCount == 1,

                    InputMode.OptionOrCustom =>
                        (hasOption && customValueCount == 0) ||
                        (!hasOption && customValueCount == 1),

                    _ => false
                };

                if (!matchesInputMode)
                {
                    return ValidationErrors.InvalidRequest(
                        $"Giá trị của thuộc tính " +
                        $"'{attribute.AttributeName ?? attribute.AttributeId.ToString()}' " +
                        $"không phù hợp với InputMode '{attribute.InputMode}'.");
                }

                // Tiêu chí 7: Option phải thuộc chính Attribute
                if (hasOption)
                {
                    var optionBelongsToAttribute =
                        attribute.ProductAttributeOptions?.Any(
                            option => option.OptionId == request.OptionId.Value)
                        == true;

                    if (!optionBelongsToAttribute)
                    {
                        return ValidationErrors.InvalidRequest(
                            $"Option {request.OptionId} không thuộc thuộc tính " +
                            $"'{attribute.AttributeName ?? attribute.AttributeId.ToString()}'.");
                    }

                    // Chọn OptionId thì không cần kiểm tra DataType,
                    // vì không có custom value.
                    continue;
                }

                // Tiêu chí 8: Custom value phải khớp DataType
                var matchesDataType = attribute.DataType switch
                {
                    DataType.Text =>
                        !string.IsNullOrWhiteSpace(request.ValueText) &&
                        !request.ValueNumber.HasValue &&
                        !request.ValueBoolean.HasValue,

                    DataType.Number =>
                        request.ValueText is null &&
                        request.ValueNumber.HasValue &&
                        !request.ValueBoolean.HasValue,

                    DataType.Boolean =>
                        request.ValueText is null &&
                        !request.ValueNumber.HasValue &&
                        request.ValueBoolean.HasValue,

                    _ => false
                };

                if (!matchesDataType)
                {
                    return ValidationErrors.InvalidRequest(
                        $"Giá trị của thuộc tính " +
                        $"'{attribute.AttributeName ?? attribute.AttributeId.ToString()}' " +
                        $"không đúng DataType '{attribute.DataType}'.");
                }
            }

            return null;
        }

        private async Task<Error?> ValidateProductDataAsync(Guid categoryId, Guid productTypeId, Guid? brandId, IEnumerable<ProductAttributeValueRequest>? attributeValues, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Category
            var category = await _categoryRepository.GetByIdAsync(
                categoryId,
                cancellationToken);

            if (category is null)
                return ProductErrors.InvalidCategory;

            // Phải load cả Attribute và Option để validation
            var productType =
                await _productTypeRepository.GetWithAttributesAndOptionsAsync(
                    productTypeId,
                    cancellationToken);

            if (productType is null || productType.CategoryId != categoryId)
                return ProductErrors.InvalidProductType;

            // 2. Kiểm tra Brand
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(
                    brandId.Value,
                    cancellationToken);

                if (brand is null)
                    return ProductErrors.InvalidBrand;
            }

            // 3. Kiểm tra toàn bộ AttributeValue
            return ValidateAttributeValues(productType, attributeValues);
        }

        // Lưu tập thuộc tính động của sản phẩm
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
                ValueText = x.ValueText?.Trim(),
                ValueNumber = x.ValueNumber,
                ValueBoolean = x.ValueBoolean
            }).ToList();

            if (values.Count == 0)
                return;

            await _attributeValueRepository.AddRangeAsync(values, cancellationToken);
        }

        private async Task UpdateAttributeValuesAsync(
            Guid productId, List<ProductAttributeValueRequest>? attributeValues, CancellationToken cancellationToken)
        {
            // 1. Xóa toàn bộ thuộc tính cũ theo ProductId
            await _attributeValueRepository.RemoveByProductIdAsync(productId, cancellationToken);

            // 2. Thêm lại danh sách thuộc tính mới (nếu có)
            await SaveAttributeValuesAsync(productId, attributeValues, cancellationToken);
        }


    }
}
