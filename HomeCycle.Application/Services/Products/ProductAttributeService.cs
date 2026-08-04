using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Categories;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.DTOs.Responses.Products;
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
    public class ProductAttributeService : IProductAttributeService
    {
        private readonly IProductAttributeRepository _attributeRepository;
        private readonly IProductAttributeOptionRepository _optionRepository;
        private readonly IProductAttributeValueRepository _attributeValueRepository;
        private readonly IProductTypeRepository _productTypeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAttributeRequest> _createValidator;
        private readonly IValidator<UpdateAttributeRequest> _updateValidator;

        public ProductAttributeService(
            IProductAttributeRepository attributeRepository,
            IProductAttributeOptionRepository optionRepository,
            IProductAttributeValueRepository attributeValueRepository,
            IProductTypeRepository productTypeRepositor,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateAttributeRequest> createValidator,
            IValidator<UpdateAttributeRequest> updateValidator
            )
        {
            _attributeRepository = attributeRepository;
            _optionRepository = optionRepository;
            _attributeValueRepository = attributeValueRepository;
            _productTypeRepository = productTypeRepositor;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<ProductAttributeResponse>> CreateAsync(
        Guid productTypeId, CreateAttributeRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<ProductAttributeResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var productType = await _productTypeRepository.GetByIdAsync(productTypeId, cancellationToken);
            if (productType is null)
                return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.ProductTypeNotFound);

            var normalizedAttributeName = request.AttributeName.Trim();

            var isDuplicate = await _attributeRepository.ExistsByNameAsync(
                productTypeId,
                normalizedAttributeName,
                cancellationToken);
            if (isDuplicate)
                return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.AttributeAlreadyExists);

            var attributeId = Guid.NewGuid();

            // Map dữ liệu nghiệp vụ từ request
            var attribute = _mapper.Map<product_attribute>(request);

            // Gán các trường hệ thống
            attribute.AttributeId = attributeId;
            attribute.ProductTypeId = productTypeId;

            await _attributeRepository.AddAsync(
                attribute,
                cancellationToken);

            var createdOptions = new List<product_attribute_option>();

            if ((request.InputMode == InputMode.OptionOnly || request.InputMode == InputMode.OptionOrCustom)
                && request.Options is { Count: > 0 })
            {
                foreach (var optionRequest in request.Options)
                {
                    var option = _mapper.Map<product_attribute_option>(optionRequest);
                    option.OptionId = Guid.NewGuid();
                    option.AttributeId = attributeId;

                    await _optionRepository.AddAsync(option, cancellationToken);
                    createdOptions.Add(option);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<ProductAttributeResponse>(attribute);

            response.Options = _mapper.Map<List<ProductAttributeOptionResponse>>(createdOptions)
                .OrderBy(option => option.DisplayOrder)
                .ToList();

            return Result<ProductAttributeResponse>.Success(response);
        }

        public async Task<Result<ProductAttributeResponse>> UpdateAsync(
        Guid attributeId, UpdateAttributeRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<ProductAttributeResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var existing = await _attributeRepository.GetByIdAsync(attributeId, cancellationToken);
            if (existing is null)
                return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.AttributeNotFound);

            // Cân nhắc: nếu Attribute đã có Product dùng (product_attribute_value tồn tại),
            // đổi DataType sẽ khiến dữ liệu cũ (VD: ValueNumber) trở nên vô nghĩa so với DataType mới.
            // Xem mục cảnh báo bên dưới.

            // Chặn sửa DataType/InputMode khi Attribute đã có dữ liệu sử dụng
            var inUse = await _attributeValueRepository.ExistsByAttributeIdAsync(attributeId, cancellationToken);
            if (inUse)
            {
                if (existing.DataType != request.DataType)
                    return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.CannotChangeDataTypeInUse);

                if (existing.InputMode != request.InputMode)
                    return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.CannotChangeInputModeInUse);
            }

            request.AttributeName = request.AttributeName.Trim();
            request.Unit = request.Unit?.Trim();

            _mapper.Map(request, existing);

            await _attributeRepository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<ProductAttributeResponse>(existing);

            if (HasOptionsMode(existing.InputMode))
            {
                var options = await _optionRepository.GetByAttributeAsync(
                    attributeId,
                    cancellationToken);

                response.Options = _mapper
                    .Map<List<ProductAttributeOptionResponse>>(options)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();
            }
            else
            {
                response.Options = [];
            }

            return Result<ProductAttributeResponse>.Success(response);
        }

        public async Task<Result<ProductAttributeResponse>> GetByIdAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            var attribute = await _attributeRepository.GetByIdAsync(attributeId, cancellationToken);

            if (attribute == null)
            {
                return Result<ProductAttributeResponse>.Fail(ProductTypeErrors.AttributeNotFound);
            }

            var response = _mapper.Map<ProductAttributeResponse>(attribute);

            if (HasOptionsMode(attribute.InputMode))
            {
                var options = await _optionRepository.GetByAttributeAsync(
                    attributeId,
                    cancellationToken);

                response.Options = _mapper
                    .Map<List<ProductAttributeOptionResponse>>(options)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();
            }

            //return Result<ProductAttributeResponse>.Success(_mapper.Map<ProductAttributeResponse>(attribute));
            return Result<ProductAttributeResponse>.Success(response);
        }

        public async Task<Result<bool>> DeleteAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            // Chặn xóa nếu đã có Product sử dụng
            var inUse = await _attributeValueRepository.ExistsByAttributeIdAsync(attributeId, cancellationToken);
            if (inUse)
                return Result<bool>.Fail(ProductTypeErrors.AttributeInUse);

            var deleted = await _attributeRepository.DeleteAsync(attributeId, cancellationToken);
            if (!deleted)
                return Result<bool>.Fail(ProductTypeErrors.AttributeNotFound);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<IReadOnlyList<ProductAttributeResponse>>> GetByProductTypeAsync(
            Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var attributes = await _attributeRepository.GetByProductTypeAsync(productTypeId, cancellationToken);
            var result = await BuildResponsesAsync(attributes, cancellationToken);
            return Result<IReadOnlyList<ProductAttributeResponse>>.Success(result);
        }

        public async Task<Result<IReadOnlyList<AttributeFilterOptionResponse>>> GetFilterableAttributesAsync(
            Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var attributes = await _attributeRepository.GetByProductTypeAsync(productTypeId, cancellationToken);

            var filterable = attributes.Where(x => x.IsFilterable).ToList();
            var result = new List<AttributeFilterOptionResponse>();

            foreach (var attr in filterable)
            {
                var response = new AttributeFilterOptionResponse
                {
                    AttributeId = attr.AttributeId,
                    AttributeName = attr.AttributeName ?? string.Empty,
                    DataType = (DataType)(attr.DataType ?? 0),
                    Unit = attr.Unit
                };

                // Chỉ Attribute có chế độ nhập Option (Dropdown/RadioButton) mới cần load Option —
                // CustomOnly (nhập tay Text/Number/Boolean) không có Option nên bỏ qua để tránh query thừa.
                if (HasOptionsMode(attr.InputMode))
                {
                    var options = await _optionRepository.GetByAttributeAsync(attr.AttributeId, cancellationToken);
                    if (options.Count > 0)
                    {
                        response.Options = options
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new AttributeOptionItem
                            {
                                OptionId = o.OptionId,
                                OptionValue = o.OptionValue ?? string.Empty
                            })
                            .ToList();
                    }
                }

                result.Add(response);
            }

            return Result<IReadOnlyList<AttributeFilterOptionResponse>>.Success(result);
        }

        private async Task<List<ProductAttributeResponse>> BuildResponsesAsync(
            IEnumerable<product_attribute> attributes, CancellationToken cancellationToken)
        {
            var result = new List<ProductAttributeResponse>();

            foreach (var attr in attributes.OrderBy(x => x.DisplayOrder))
            {
                var response = new ProductAttributeResponse
                {
                    AttributeId = attr.AttributeId,
                    AttributeName = attr.AttributeName ?? string.Empty,
                    DataType = attr.DataType,
                    Unit = attr.Unit,
                    DisplayOrder = attr.DisplayOrder,
                    IsFilterable = attr.IsFilterable,
                    IsRequired = attr.IsRequired,
                    InputMode = attr.InputMode
                };

                // Chỉ query Option khi InputMode cho phép chọn từ danh sách (OptionOnly/OptionOrCustom)
                if (HasOptionsMode(attr.InputMode))
                {
                    var options = await _optionRepository.GetByAttributeAsync(attr.AttributeId, cancellationToken);
                    response.Options = options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new ProductAttributeOptionResponse
                        {
                            OptionId = o.OptionId,
                            OptionValue = o.OptionValue ?? string.Empty,
                            DisplayOrder = o.DisplayOrder
                            //IsDefault = o.IsDefault
                        })
                        .ToList();
                }

                result.Add(response);
            }

            return result;
        }

        private static bool HasOptionsMode(InputMode? inputMode)
        {
            return inputMode == InputMode.OptionOnly || inputMode == InputMode.OptionOrCustom;
        }
    }
}
