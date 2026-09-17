using FluentValidation;
using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Validations.AI;

public sealed class AiPriceDraftRequestValidator : AbstractValidator<AiPriceDraftRequest>
{
    public AiPriceDraftRequestValidator(
        IProductAttributeRepository attributes,
        IProductAttributeOptionRepository options)
    {
        RuleFor(x => x.Product).NotNull().WithMessage("Cần thông tin sản phẩm để gợi ý giá.");
        When(x => x.Product != null, () =>
        {
            RuleFor(x => x.Product!.ProductTypeId).NotEmpty().WithMessage("Cần chọn loại sản phẩm.");
            RuleFor(x => x.Product!.BrandId).NotNull().NotEqual(Guid.Empty)
                .WithMessage("Cần chọn thương hiệu.");
            RuleFor(x => x.Product!.ProductName).NotEmpty().MaximumLength(255)
                .WithMessage("Tên sản phẩm bắt buộc và tối đa 255 ký tự.");
            RuleFor(x => x.Product!.ModelNumber).Cascade(CascadeMode.Stop)
                .NotEmpty().MaximumLength(100)
                .Must(value => value!.Count(char.IsAsciiLetterOrDigit) >= 1)
                .WithMessage("Cần mã model hợp lệ, tối đa 100 ký tự.");
            RuleFor(x => x.Product!.FunctionalityStatus).NotNull().IsInEnum()
                .WithMessage("Cần chọn tình trạng hoạt động hợp lệ.");
            RuleFor(x => x.Product!.DamageLevel).NotNull().IsInEnum()
                .WithMessage("Cần chọn mức hư hại hợp lệ.");
            RuleFor(x => x.Product!.UsageDuration).GreaterThanOrEqualTo(0)
                .When(x => x.Product!.UsageDuration.HasValue)
                .WithMessage("Thời gian sử dụng không được âm.");
            RuleFor(x => x.Product!.AttributeValues).NotNull();
            When(x => x.Product!.AttributeValues != null, () =>
            {
                RuleFor(x => x.Product!.AttributeValues.Count).LessThanOrEqualTo(30)
                    .WithMessage("Chỉ gửi tối đa 30 thuộc tính sản phẩm.");
                RuleFor(x => x.Product!.AttributeValues)
                    .Must(values => values.All(v => v != null) &&
                                    values.Select(v => v.AttributeId).Distinct().Count() == values.Count)
                    .WithMessage("Thuộc tính không được null hoặc trùng AttributeId.");
            });
        });

        RuleFor(x => x).CustomAsync(async (request, context, cancellationToken) =>
        {
            // Structural errors are already reported above; do not query unsafe collections.
            if (request.Product is null || request.Product.ProductTypeId == Guid.Empty ||
                request.Product.AttributeValues is null || request.Product.AttributeValues.Count > 30 ||
                request.Product.AttributeValues.Any(x => x is null) ||
                request.Product.AttributeValues.Select(x => x.AttributeId).Distinct().Count() !=
                request.Product.AttributeValues.Count) return;
            var product = request.Product;
            var definitions = await attributes.GetByProductTypeAsync(product.ProductTypeId, cancellationToken);
            var map = definitions.ToDictionary(x => x.AttributeId);
            foreach (var required in definitions.Where(x => x.IsRequired))
            {
                if (!product.AttributeValues.Any(x => x.AttributeId == required.AttributeId))
                    context.AddFailure("Product.AttributeValues", $"Thiếu thuộc tính bắt buộc: {required.AttributeName}.");
            }

            for (var i = 0; i < product.AttributeValues.Count; i++)
            {
                var value = product.AttributeValues[i];
                var path = $"Product.AttributeValues[{i}]";
                if (value.AttributeId == Guid.Empty || !map.TryGetValue(value.AttributeId, out var definition))
                {
                    context.AddFailure(path, "Thuộc tính không tồn tại hoặc không thuộc loại sản phẩm đã chọn.");
                    continue;
                }

                var valueCount = (value.OptionId.HasValue ? 1 : 0) + (value.ValueNumber.HasValue ? 1 : 0) +
                                 (value.ValueBoolean.HasValue ? 1 : 0) + (!string.IsNullOrWhiteSpace(value.ValueText) ? 1 : 0);
                if (valueCount != 1)
                {
                    context.AddFailure(path, "Mỗi thuộc tính phải có đúng một giá trị hoặc một tùy chọn.");
                    continue;
                }
                if (value.ValueText?.Length > 300 || value.ValueNumber < 0)
                {
                    context.AddFailure(path, "Giá trị số không được âm; giá trị chữ tối đa 300 ký tự.");
                    continue;
                }
                if (value.OptionId.HasValue)
                {
                    var option = value.OptionId == Guid.Empty ? null :
                        await options.GetByIdAsync(value.OptionId.Value, cancellationToken);
                    if (definition.InputMode == InputMode.CustomOnly || option?.AttributeId != value.AttributeId)
                        context.AddFailure(path, "Tùy chọn không hợp lệ cho thuộc tính này.");
                }
                else
                {
                    var matchesType = definition.DataType switch
                    {
                        DataType.Number => value.ValueNumber.HasValue,
                        DataType.Boolean => value.ValueBoolean.HasValue,
                        DataType.Text => !string.IsNullOrWhiteSpace(value.ValueText),
                        _ => false
                    };
                    if (definition.InputMode == InputMode.OptionOnly || !matchesType)
                        context.AddFailure(path, "Giá trị không đúng kiểu dữ liệu hoặc chế độ nhập của thuộc tính.");
                }
            }
        });
    }
}
