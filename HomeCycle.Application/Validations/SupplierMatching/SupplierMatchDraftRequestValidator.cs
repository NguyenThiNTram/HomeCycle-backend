using FluentValidation;
using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Validations.SupplierMatching;

public sealed class SupplierMatchDraftRequestValidator : AbstractValidator<SupplierMatchDraftRequest>
{
    public SupplierMatchDraftRequestValidator(
        IProductAttributeRepository attributes,
        IProductAttributeOptionRepository options)
    {
        RuleFor(x => x)
            .Must(x => x.ProductTypeId.HasValue || x.CategoryId.HasValue)
            .WithMessage("Cần chọn loại sản phẩm hoặc danh mục sản phẩm.");
        RuleFor(x => x.ProductTypeId).NotEqual(Guid.Empty).When(x => x.ProductTypeId.HasValue);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.BrandId).NotEqual(Guid.Empty).When(x => x.BrandId.HasValue);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Số lượng cần mua phải lớn hơn 0.");
        RuleFor(x => x.PriceFrom).GreaterThanOrEqualTo(0).When(x => x.PriceFrom.HasValue);
        RuleFor(x => x.PriceTo).GreaterThanOrEqualTo(0).When(x => x.PriceTo.HasValue);
        RuleFor(x => x)
            .Must(x => !x.PriceFrom.HasValue || !x.PriceTo.HasValue || x.PriceFrom <= x.PriceTo)
            .WithMessage("Giá tối thiểu không được lớn hơn giá tối đa.");
        RuleFor(x => x.ModelNumber)
            .MaximumLength(100)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Any(char.IsLetterOrDigit))
            .WithMessage("Mã model tối đa 100 ký tự và phải chứa ít nhất một chữ cái hoặc chữ số.");
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.FunctionalityStatus).IsInEnum().When(x => x.FunctionalityStatus.HasValue);
        RuleFor(x => x.DamageLevel).IsInEnum().When(x => x.DamageLevel.HasValue);
        RuleFor(x => x.UsageDuration).GreaterThanOrEqualTo(0).When(x => x.UsageDuration.HasValue);
        RuleFor(x => x.AttributeValues).NotNull();
        RuleFor(x => x.AttributeValues.Count).LessThanOrEqualTo(30)
            .WithMessage("Chỉ gửi tối đa 30 thuộc tính sản phẩm.");
        RuleFor(x => x.AttributeValues)
            .Must(values => values.Select(x => x.AttributeId).Distinct().Count() == values.Count)
            .WithMessage("Thuộc tính không được trùng AttributeId.");
        RuleFor(x => x.AdvancedFilters!.MinimumSellerRating)
            .InclusiveBetween(0, 5)
            .When(x => x.AdvancedFilters?.MinimumSellerRating.HasValue == true);

        RuleFor(x => x).CustomAsync(async (request, context, cancellationToken) =>
        {
            if (request.AttributeValues.Count > 30 ||
                request.AttributeValues.Select(x => x.AttributeId).Distinct().Count() != request.AttributeValues.Count)
                return;

            if (!request.ProductTypeId.HasValue)
            {
                if (request.AttributeValues.Count > 0)
                    context.AddFailure(nameof(request.ProductTypeId),
                        "Cần chọn loại sản phẩm khi gửi thuộc tính động.");
                return;
            }

            var definitions = await attributes.GetByProductTypeAsync(
                request.ProductTypeId.Value, cancellationToken);
            var definitionMap = definitions.ToDictionary(x => x.AttributeId);

            foreach (var required in definitions.Where(x => x.IsRequired))
            {
                if (!request.AttributeValues.Any(x => x.AttributeId == required.AttributeId))
                    context.AddFailure(nameof(request.AttributeValues),
                        $"Thiếu thuộc tính bắt buộc: {required.AttributeName}.");
            }

            for (var index = 0; index < request.AttributeValues.Count; index++)
            {
                var value = request.AttributeValues[index];
                var path = $"AttributeValues[{index}]";
                if (value.AttributeId == Guid.Empty ||
                    !definitionMap.TryGetValue(value.AttributeId, out var definition))
                {
                    context.AddFailure(path,
                        "Thuộc tính không tồn tại hoặc không thuộc loại sản phẩm đã chọn.");
                    continue;
                }

                var valueCount = (value.OptionId.HasValue ? 1 : 0) +
                                 (value.ValueNumber.HasValue ? 1 : 0) +
                                 (value.ValueBoolean.HasValue ? 1 : 0) +
                                 (!string.IsNullOrWhiteSpace(value.ValueText) ? 1 : 0);
                if (valueCount != 1)
                {
                    context.AddFailure(path, "Mỗi thuộc tính phải có đúng một giá trị hoặc một tùy chọn.");
                    continue;
                }

                if (value.ValueText?.Length > 300 || value.ValueNumber < 0)
                {
                    context.AddFailure(path,
                        "Giá trị số không được âm; giá trị chữ tối đa 300 ký tự.");
                    continue;
                }

                if (value.OptionId.HasValue)
                {
                    var option = value.OptionId == Guid.Empty
                        ? null
                        : await options.GetByIdAsync(value.OptionId.Value, cancellationToken);
                    if (definition.InputMode == InputMode.CustomOnly ||
                        option?.AttributeId != value.AttributeId)
                        context.AddFailure(path, "Tùy chọn không hợp lệ cho thuộc tính này.");
                    continue;
                }

                var matchesType = definition.DataType switch
                {
                    DataType.Number => value.ValueNumber.HasValue,
                    DataType.Boolean => value.ValueBoolean.HasValue,
                    DataType.Text => !string.IsNullOrWhiteSpace(value.ValueText),
                    _ => false
                };
                if (definition.InputMode == InputMode.OptionOnly || !matchesType)
                    context.AddFailure(path,
                        "Giá trị không đúng kiểu dữ liệu hoặc chế độ nhập của thuộc tính.");
            }
        });
    }
}
