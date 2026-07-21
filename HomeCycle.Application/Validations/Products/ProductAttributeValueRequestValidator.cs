using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class ProductAttributeValueRequestValidator : AbstractValidator<ProductAttributeValueRequest>
    {
        public ProductAttributeValueRequestValidator()
        {
            RuleFor(x => x.AttributeId)
                .NotEmpty()
                .WithMessage("Attribute is required.");

            RuleFor(x => x.InputType)
                .IsInEnum()
                .WithMessage("Invalid input type.");

            // 1. Nếu InputType == TextBox (Nhập văn bản tự do)
            When(x => x.InputType == InputType.TextBox, () =>
            {
                RuleFor(x => x.ValueText)
                    .NotEmpty().WithMessage("Giá trị văn bản không được để trống.")
                    .MaximumLength(1000).WithMessage("Văn bản không được vượt quá 1000 ký tự.");

                RuleFor(x => x.OptionId).Null().WithMessage("Kiểu nhập tự do không được kèm OptionId.");
                RuleFor(x => x.ValueNumber).Null().WithMessage("Kiểu nhập tự do không được kèm ValueNumber.");
                RuleFor(x => x.ValueBoolean).Null().WithMessage("Kiểu nhập tự do không được kèm ValueBoolean.");
            });

            // 2. Nếu InputType == NumberBox (Nhập số)
            When(x => x.InputType == InputType.NumberBox, () =>
            {
                RuleFor(x => x.ValueNumber)
                    .NotNull().WithMessage("Giá trị số không được để trống.")
                    .GreaterThanOrEqualTo(0).WithMessage("Giá trị số phải lớn hơn hoặc bằng 0.");

                RuleFor(x => x.OptionId).Null().WithMessage("Kiểu nhập số không được kèm OptionId.");
                RuleFor(x => x.ValueText).Null().WithMessage("Kiểu nhập số không được kèm ValueText.");
                RuleFor(x => x.ValueBoolean).Null().WithMessage("Kiểu nhập số không được kèm ValueBoolean.");
            });

            // 3. Nếu InputType == CheckBox (Đúng / Sai)
            When(x => x.InputType == InputType.CheckBox, () =>
            {
                RuleFor(x => x.ValueBoolean)
                    .NotNull().WithMessage("Giá trị Boolean (Đúng/Sai) không được để trống.");

                RuleFor(x => x.OptionId).Null().WithMessage("Kiểu CheckBox không được kèm OptionId.");
                RuleFor(x => x.ValueText).Null().WithMessage("Kiểu CheckBox không được kèm ValueText.");
                RuleFor(x => x.ValueNumber).Null().WithMessage("Kiểu CheckBox không được kèm ValueNumber.");
            });

            // 4. Nếu InputType == Dropdown hoặc RadioButton (Chọn từ danh sách)
            When(x => x.InputType == InputType.Dropdown || x.InputType == InputType.RadioButton, () =>
            {
                RuleFor(x => x.OptionId)
                    .NotNull().WithMessage("Vui lòng chọn một tùy chọn (OptionId).")
                    .NotEmpty().WithMessage("OptionId không hợp lệ.");

                RuleFor(x => x.ValueText).Null().WithMessage("Kiểu lựa chọn không được kèm ValueText.");
                RuleFor(x => x.ValueNumber).Null().WithMessage("Kiểu lựa chọn không được kèm ValueNumber.");
                RuleFor(x => x.ValueBoolean).Null().WithMessage("Kiểu lựa chọn không được kèm ValueBoolean.");
            });

            When(x => x.ValueText != null, () =>
            {
                RuleFor(x => x.ValueText)
                    .MaximumLength(1000)
                    .WithMessage("Text value cannot exceed 1000 characters.");
            });

            When(x => x.ValueNumber.HasValue, () =>
            {
                RuleFor(x => x.ValueNumber!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Numeric value must be greater than or equal to 0.");
            });

            RuleFor(x => x)
                .Must(HaveOnlyOneValue)
                .WithMessage("Only one value type (OptionId, ValueText, ValueNumber, ValueBoolean) is allowed.");
        }

        private static bool HaveOnlyOneValue(ProductAttributeValueRequest request)
        {
            var count = 0;

            if (request.OptionId.HasValue)
                count++;

            if (!string.IsNullOrWhiteSpace(request.ValueText))
                count++;

            if (request.ValueNumber.HasValue)
                count++;

            if (request.ValueBoolean.HasValue)
                count++;

            return count <= 1;
        }
    }
}
