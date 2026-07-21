using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
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
    public class ProductTypeService : IProductTypeService
    {
        private readonly IProductTypeRepository _productTypeRepository;
        private readonly IProductAttributeRepository _productAttributeRepository;
        private readonly IProductAttributeOptionRepository _productAttributeOptionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductTypeService> _logger;

        private readonly IValidator<CreateProductTypeRequest> _createValidator;
        private readonly IValidator<UpdateProductTypeRequest> _updateValidator;

        public ProductTypeService(
            IProductTypeRepository productTypeRepository,
            IProductAttributeRepository productAttributeRepository,
            IProductAttributeOptionRepository productAttributeOptionRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateProductTypeRequest> createValidator,
            IValidator<UpdateProductTypeRequest> updateValidator,
            ILogger<ProductTypeService> logger)
        {
            _productTypeRepository = productTypeRepository;
            _productAttributeRepository = productAttributeRepository;
            _productAttributeOptionRepository = productAttributeOptionRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        public async Task<Result<ProductTypeResponse>> CreateAsync(CreateProductTypeRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors.Select(x => x.ErrorMessage));
                return Result<ProductTypeResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var isDuplicate = await _productTypeRepository.ExistsByNameAsync(
                request.CategoryId, request.ProductTypeName, cancellationToken);

            if (isDuplicate)
                return Result<ProductTypeResponse>.Fail(ProductTypeErrors.ProductTypeAlreadyExists);

            var productType = _mapper.Map<product_type>(request);
            var now = DateTime.UtcNow;

            productType.ProductTypeId = Guid.NewGuid();
            productType.CreatedAt = now;
            productType.IsActive = true;

            foreach (var attrDto in request.Attributes)
            {
                var attributeId = Guid.NewGuid();

                var domainAttribute = new product_attribute(attributeId, productType.ProductTypeId)
                {
                    AttributeName = attrDto.AttributeName.Trim(),
                    DataType = attrDto.DataType,
                    Unit = attrDto.Unit?.Trim(),
                    DisplayOrder = attrDto.DisplayOrder,
                    IsFilterable = attrDto.IsFilterable,
                    IsRequired = attrDto.IsRequired,
                    ProductAttributeOptions = new List<product_attribute_option>()
                };

                foreach (var optDto in attrDto.Options)
                {
                    var optionId = Guid.NewGuid();

                    domainAttribute.ProductAttributeOptions.Add(
                        new product_attribute_option(optionId, attributeId)
                        {
                            OptionValue = optDto.OptionValue.Trim(),
                            DisplayOrder = optDto.DisplayOrder,
                            IsDefault = optDto.IsDefault
                        });
                }

                productType.ProductAttributes.Add(domainAttribute);
            }

            await _productTypeRepository.AddAsync(productType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<ProductTypeDetailResponse>(productType);

            return Result<ProductTypeResponse>.Success(response);
        }

        public async Task<Result<ProductTypeResponse>> UpdateAsync(Guid productTypeId, UpdateProductTypeRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors.Select(x => x.ErrorMessage));
                return Result<ProductTypeResponse>.Fail(ValidationErrors.InvalidRequest(errors));
            }

            var aggregate = await _productTypeRepository.AggregateUpdateAsync(productTypeId, cancellationToken);
            if (aggregate is null)
                return Result<ProductTypeResponse>.Fail(ProductTypeErrors.ProductTypeNotFound);

            var exists = await _productTypeRepository.ExistsByNameAsync(aggregate.CategoryId, request.ProductTypeName, cancellationToken);

            if (exists && !aggregate.ProductTypeName!.Equals(request.ProductTypeName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Result<ProductTypeResponse>.Fail(ProductTypeErrors.ProductTypeAlreadyExists);
            }

            aggregate.ProductTypeName = request.ProductTypeName.Trim();
            aggregate.Description = request.Description?.Trim();
            aggregate.IsActive = request.IsActive;

            // ==========================================
            // KỸ THUẬT GRAPH DIFFING (BẢO VỆ ĐỒ THỊ DỮ LIỆU)
            // ==========================================

            // --- BƯỚC A: XỬ LÝ TẦNG PRODUCT_ATTRIBUTE ---
            var incomingAttrIds = request.Attributes.Where(a => a.AttributeId.HasValue).Select(a => a.AttributeId!.Value).ToHashSet();

            // Lọc ra các Attribute bị xóa (Có trong DB cũ nhưng không có trong Request mới)
            var attributesToRemove = aggregate.ProductAttributes.Where(a => !incomingAttrIds.Contains(a.AttributeId)).ToList();
            foreach (var attr in attributesToRemove)
            {
                aggregate.ProductAttributes.Remove(attr);
            }

            // Duyệt danh sách Request để Xử lý Cập nhật hoặc Thêm mới Attribute
            foreach (var attrDto in request.Attributes)
            {
                if (attrDto.AttributeId.HasValue) // Tình huống: Cập nhật Attribute đang có
                {
                    var existingAttr = aggregate.ProductAttributes.FirstOrDefault(a => a.AttributeId == attrDto.AttributeId.Value);
                    if (existingAttr != null)
                    {
                        // Cập nhật các trường dữ liệu cơ bản
                        existingAttr.AttributeName = attrDto.AttributeName.Trim();
                        existingAttr.DataType = attrDto.DataType;
                        existingAttr.Unit = attrDto.Unit?.Trim();
                        existingAttr.DisplayOrder = attrDto.DisplayOrder;
                        existingAttr.IsFilterable = attrDto.IsFilterable;
                        existingAttr.IsRequired = attrDto.IsRequired;

                        // --- BƯỚC B: XỬ LÝ TẦNG CON CỦA ATTRIBUTE (PRODUCT_ATTRIBUTE_OPTION) ---
                        var incomingOptIds = attrDto.Options.Where(o => o.OptionId.HasValue).Select(o => o.OptionId!.Value).ToHashSet();

                        // Xóa các Option không còn xuất hiện trong Request DTO
                        var optionsToRemove = existingAttr.ProductAttributeOptions.Where(o => !incomingOptIds.Contains(o.OptionId)).ToList();
                        foreach (var opt in optionsToRemove)
                        {
                            existingAttr.ProductAttributeOptions.Remove(opt);
                        }

                        // Cập nhật hoặc Thêm mới Option
                        foreach (var optDto in attrDto.Options)
                        {
                            if (optDto.OptionId.HasValue) // Cập nhật Option cũ
                            {
                                var existingOpt = existingAttr.ProductAttributeOptions.FirstOrDefault(o => o.OptionId == optDto.OptionId.Value);
                                if (existingOpt != null)
                                {
                                    existingOpt.OptionValue = optDto.OptionValue.Trim();
                                    existingOpt.DisplayOrder = optDto.DisplayOrder;
                                    existingOpt.IsDefault = optDto.IsDefault;
                                }
                            }
                            else // Thêm Option mới vào Attribute đang tồn tại
                            {
                                existingAttr.ProductAttributeOptions.Add(
                                    new product_attribute_option(Guid.NewGuid(), existingAttr.AttributeId)
                                    {
                                        OptionValue = optDto.OptionValue.Trim(),
                                        DisplayOrder = optDto.DisplayOrder,
                                        IsDefault = optDto.IsDefault
                                    });
                            }
                        }
                    }
                }
                else // Tình huống: Thêm mới hoàn toàn một Attribute kèm Option
                {
                    var newAttrId = Guid.NewGuid();
                    var newAttribute = new product_attribute(newAttrId, aggregate.ProductTypeId)
                    {
                        AttributeName = attrDto.AttributeName.Trim(),
                        DataType = attrDto.DataType,
                        Unit = attrDto.Unit?.Trim(),
                        DisplayOrder = attrDto.DisplayOrder,
                        IsFilterable = attrDto.IsFilterable,
                        IsRequired = attrDto.IsRequired,
                        ProductAttributeOptions = new List<product_attribute_option>()
                    };

                    foreach (var optDto in attrDto.Options)
                    {
                        newAttribute.ProductAttributeOptions.Add(
                            new product_attribute_option(Guid.NewGuid(), newAttrId)
                            {
                                OptionValue = optDto.OptionValue.Trim(),
                                DisplayOrder = optDto.DisplayOrder,
                                IsDefault = optDto.IsDefault
                            });
                    }

                    aggregate.ProductAttributes.Add(newAttribute);
                }
            }

            await _productTypeRepository.AggregateUpdateAsync(productTypeId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<ProductTypeResponse>(aggregate);
            return Result<ProductTypeResponse>.Success(response);
        }

        public async Task<Result<bool>> DeleteAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var modified = await _productTypeRepository.DeleteAsync(productTypeId, cancellationToken);
            if (!modified)
                return Result<bool>.Fail(ProductTypeErrors.ProductTypeNotFound);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<PagedResult<ProductTypeResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var pagedTypes = await _productTypeRepository.GetAllAsync(request, cancellationToken);

            var dto = new PagedResult<ProductTypeResponse>
            {
                Items = pagedTypes.Items
                    .Select(x => _mapper.Map<ProductTypeResponse>(x))
                    .ToList(),
                PageNumber = pagedTypes.PageNumber,
                PageSize = pagedTypes.PageSize,
                TotalCount = pagedTypes.TotalCount,
                TotalPages = pagedTypes.TotalPages
            };

            return Result<PagedResult<ProductTypeResponse>>.Success(dto);
        }

        public async Task<Result<IEnumerable<product_type>>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var types = await _productTypeRepository.GetByCategoryIdAsync(categoryId, cancellationToken);
            return Result<IEnumerable<product_type>>.Success(types);
        }

        public async Task<Result<ProductTypeDetailResponse>> GetByIdAsync(Guid productTypeId, CancellationToken cancellationToken = default)
        {
            var type = await _productTypeRepository.GetByIdAsync(productTypeId, cancellationToken);
            if (type is null)
                return Result<ProductTypeDetailResponse>.Fail(ProductTypeErrors.ProductTypeNotFound);

            var dto = _mapper.Map<ProductTypeDetailResponse>(type);
            return Result<ProductTypeDetailResponse>.Success(dto);
        }

        public async Task<Result<PagedResult<ProductTypeResponse>>> SearchAsync(ProductTypeSearchRequest request, CancellationToken cancellationToken = default)
        {
            var pagedTypes = await _productTypeRepository.SearchAsync(request, cancellationToken);

            var dto = new PagedResult<ProductTypeResponse>
            {
                Items = pagedTypes.Items
                    .Select(x => _mapper.Map<ProductTypeResponse>(x))
                    .ToList(),
                PageNumber = pagedTypes.PageNumber,
                PageSize = pagedTypes.PageSize,
                TotalCount = pagedTypes.TotalCount,
                TotalPages = pagedTypes.TotalPages
            };

            return Result<PagedResult<ProductTypeResponse>>.Success(dto);
        }
 
    }

}
