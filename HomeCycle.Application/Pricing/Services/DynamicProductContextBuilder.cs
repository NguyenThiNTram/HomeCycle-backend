using System.Globalization;
using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Services.AI;
using HomeCycle.Application.Pricing.Models;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Pricing.Services;

public sealed class DynamicProductContextBuilder(
    IProductTypeRepository productTypes,
    IBrandRepository brands,
    IProductAttributeRepository attributes,
    IProductAttributeOptionRepository options) : IProductContextProvider
{
    private const int MaxContextAttributes = 8;

    public async Task<DynamicProductContext?> BuildDraftAsync(
        AiPriceDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var draft = request.Product;
        if (draft is null || draft.ProductTypeId == Guid.Empty || draft.BrandId is null ||
            draft.BrandId == Guid.Empty || string.IsNullOrWhiteSpace(draft.ProductName) ||
            string.IsNullOrWhiteSpace(draft.ModelNumber) || draft.FunctionalityStatus is null ||
            draft.DamageLevel is null)
            return null;

        var productType = await productTypes.GetByIdAsync(draft.ProductTypeId, cancellationToken);
        var brand = await brands.GetByIdAsync(draft.BrandId.Value, cancellationToken);
        if (productType is null || brand is null)
            return null;

        var definitions = await attributes.GetByProductTypeAsync(draft.ProductTypeId, cancellationToken);
        var definitionMap = definitions.ToDictionary(x => x.AttributeId);
        var contextAttributes = new List<DynamicAttributeValue>();

        foreach (var value in draft.AttributeValues)
        {
            if (!definitionMap.TryGetValue(value.AttributeId, out var definition) ||
                definition.DataType is null)
                continue;

            string? optionValue = null;
            if (value.OptionId.HasValue)
            {
                var option = await options.GetByIdAsync(value.OptionId.Value, cancellationToken);
                if (option?.AttributeId != value.AttributeId)
                    continue;
                optionValue = option.OptionValue;
            }

            var displayValue = optionValue ??
                value.ValueNumber?.ToString(CultureInfo.InvariantCulture) ??
                (value.ValueBoolean.HasValue
                    ? value.ValueBoolean.Value.ToString().ToLowerInvariant()
                    : value.ValueText?.Trim());

            if (string.IsNullOrWhiteSpace(displayValue))
                continue;

            contextAttributes.Add(new DynamicAttributeValue(
                definition.AttributeId,
                definition.AttributeName?.Trim() ?? definition.AttributeId.ToString(),
                definition.DataType.Value,
                string.IsNullOrWhiteSpace(definition.Unit) ? null : definition.Unit.Trim(),
                value.OptionId,
                displayValue,
                value.ValueNumber,
                value.ValueBoolean,
                value.ValueText?.Trim(),
                definition.IsRequired,
                definition.IsFilterable,
                definition.DisplayOrder ?? int.MaxValue));
        }

        var selectedAttributes = contextAttributes
            .OrderByDescending(x => x.IsFilterable)
            .ThenByDescending(x => x.IsRequired)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxContextAttributes)
            .ToArray();

        var normalizedModel = new string(draft.ModelNumber
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return new DynamicProductContext(
            draft.ProductTypeId,
            productType.ProductTypeName?.Trim() ?? string.Empty,
            draft.BrandId.Value,
            brand.BrandName.Trim(),
            draft.ProductName.Trim(),
            normalizedModel,
            draft.FunctionalityStatus.Value,
            draft.DamageLevel.Value,
            draft.UsageDuration,
            selectedAttributes);
    }
}
