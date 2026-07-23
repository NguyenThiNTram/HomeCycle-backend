using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.Products;
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

        public ProductAttributeService(
            IProductAttributeRepository attributeRepository,
            IProductAttributeOptionRepository optionRepository)
        {
            _attributeRepository = attributeRepository;
            _optionRepository = optionRepository;
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

                // Chỉ Attribute có Option (Dropdown/RadioButton) mới cần load Option —
                // Number/Boolean/Text tự do không có Option nên bỏ qua để tránh query thừa.
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

                result.Add(response);
            }

            return Result<IReadOnlyList<AttributeFilterOptionResponse>>.Success(result);
        }
    }
}
