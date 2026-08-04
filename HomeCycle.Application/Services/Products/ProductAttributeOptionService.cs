using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Products
{
    public class ProductAttributeOptionService : IProductAttributeOptionService
    {
        private readonly IProductAttributeRepository _attributeRepository;
        private readonly IProductAttributeOptionRepository _optionRepository;
        private readonly IProductAttributeValueRepository _attributeValueRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAttributeOptionRequest> _createValidator;
        private readonly IValidator<UpdateAttributeOptionRequest> _updateValidator;


        public ProductAttributeOptionService(
            IProductAttributeRepository attributeRepository,
            IProductAttributeOptionRepository optionRepository,
            IProductAttributeValueRepository attributeValueRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateAttributeOptionRequest> createValidator,
            IValidator<UpdateAttributeOptionRequest> updateValidator)
        {
            _attributeRepository = attributeRepository;
            _optionRepository = optionRepository;
            _attributeValueRepository = attributeValueRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }


        public async Task<Result<ProductAttributeOptionResponse>> CreateAsync(Guid attributeId,
            CreateAttributeOptionRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<ProductAttributeOptionResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var attribute = await _attributeRepository.GetByIdAsync(attributeId, cancellationToken);
            if (attribute is null)
                return Result<ProductAttributeOptionResponse>.Fail(ProductTypeErrors.AttributeNotFound);

            var isDuplicate = await _optionRepository.ExistsAsync(attributeId, request.OptionValue, cancellationToken);
            if (isDuplicate)
                return Result<ProductAttributeOptionResponse>.Fail(ProductAttributeOptionErrors.OptionAlreadyExists);

            var entity = new product_attribute_option(Guid.NewGuid(), attributeId)
            {
                OptionValue = request.OptionValue.Trim(),
                DisplayOrder = request.DisplayOrder
                //IsDefault = request.IsDefault
            };

            // Nếu Option mới được đánh dấu IsDefault, các Option khác cùng Attribute phải bỏ default
            // để đảm bảo chỉ có duy nhất 1 Option mặc định tại một thời điểm.
            //if (request.IsDefault)
            //    await ClearOtherDefaultsAsync(attributeId, excludeOptionId: null, cancellationToken);

            await _optionRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ProductAttributeOptionResponse>.Success(_mapper.Map<ProductAttributeOptionResponse>(entity));
        }

        public async Task<Result<ProductAttributeOptionResponse>> UpdateAsync(
            Guid optionId, UpdateAttributeOptionRequest request, CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                return Result<ProductAttributeOptionResponse>.Fail(
                    ValidationErrors.InvalidRequest(string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))));

            var existing = await _optionRepository.GetByIdAsync(optionId, cancellationToken);
            if (existing is null)
                return Result<ProductAttributeOptionResponse>.Fail(ProductAttributeOptionErrors.OptionNotFound);

            // Đổi tên Option nhưng trùng với Option khác cùng Attribute (loại trừ chính nó)
            var isDuplicate = await _optionRepository.ExistsAsync(existing.AttributeId, request.OptionValue, cancellationToken);
            if (isDuplicate && !existing.OptionValue!.Equals(request.OptionValue.Trim(), StringComparison.OrdinalIgnoreCase))
                return Result<ProductAttributeOptionResponse>.Fail(ProductAttributeOptionErrors.OptionAlreadyExists);

            existing.OptionValue = request.OptionValue.Trim();
            existing.DisplayOrder = request.DisplayOrder;
            //existing.IsDefault = request.IsDefault;

            //if (request.IsDefault)
            //    await ClearOtherDefaultsAsync(existing.AttributeId, excludeOptionId: optionId, cancellationToken);

            await _optionRepository.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ProductAttributeOptionResponse>.Success(_mapper.Map<ProductAttributeOptionResponse>(existing));
        }



        public async Task<Result<bool>> DeleteAsync(Guid optionId, CancellationToken cancellationToken = default)
        {
            var existing = await _optionRepository.GetByIdAsync(optionId, cancellationToken);
            if (existing is null)
                return Result<bool>.Fail(ProductAttributeOptionErrors.OptionNotFound);

            var inUse = await _attributeValueRepository.ExistsByOptionIdAsync(optionId, cancellationToken);
            if (inUse)
                return Result<bool>.Fail(ProductAttributeOptionErrors.OptionInUse);

            var deleted = await _optionRepository.DeleteAsync(optionId, cancellationToken);
            if (!deleted)
                return Result<bool>.Fail(ProductAttributeOptionErrors.OptionNotFound);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        public async Task<Result<IReadOnlyList<ProductAttributeOptionResponse>>> GetByAttributeAsync(Guid attributeId, CancellationToken cancellationToken = default)
        {
            var options = await _optionRepository.GetByAttributeAsync(attributeId, cancellationToken);
            var response = options
                .OrderBy(x => x.DisplayOrder)
                .Select(x => _mapper.Map<ProductAttributeOptionResponse>(x))
                .ToList();

            return Result<IReadOnlyList<ProductAttributeOptionResponse>>.Success(response);
        }
    }
}
