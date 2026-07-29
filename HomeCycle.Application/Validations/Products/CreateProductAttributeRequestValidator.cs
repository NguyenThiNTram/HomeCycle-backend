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
    public class CreateProductAttributeRequestValidator : AbstractValidator<CreateAttributeRequest>
    {
        public CreateProductAttributeRequestValidator()
        {
            RuleFor(x => x.AttributeName)
                .NotEmpty().WithMessage("Tên thuộc tính không được để trống.")
                .MaximumLength(255).WithMessage("Tên thuộc tính không được vượt quá 255 ký tự.");

            RuleFor(x => x.DataType)
                .IsInEnum().WithMessage("Kiểu dữ liệu (DataType) không hợp lệ.");

            RuleFor(x => x.InputMode)
                .NotNull().WithMessage("Chế độ nhập liệu (InputMode) không được để trống.")
                .IsInEnum().WithMessage("Chế độ nhập liệu (InputMode) không hợp lệ.");

            RuleFor(x => x.Unit)
                .MaximumLength(50).WithMessage("Đơn vị tính không được vượt quá 50 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue)
                .WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");

            // Ràng buộc nghiệp vụ theo InputMode
            When(x => x.InputMode == InputMode.OptionOnly || x.InputMode == InputMode.OptionOrCustom, () =>
            {
                RuleFor(x => x.Options)
                    .NotEmpty().WithMessage("Danh sách tùy chọn (Options) không được để trống khi chọn chế độ OptionOnly hoặc OptionOrCustom.");

                // Validate từng Option con trong danh sách
                RuleForEach(x => x.Options)
                    .SetValidator(new CreateAttributeOptionRequestValidator());
            });

            When(x => x.InputMode == InputMode.CustomOnly, () =>
            {
                RuleFor(x => x.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Không được truyền danh sách Options khi chế độ nhập liệu là CustomOnly.");
            });
        }
    }

    public class UpdateProductAttributeRequestValidator : AbstractValidator<UpdateAttributeRequest>
    {
        public UpdateProductAttributeRequestValidator()
        {
            RuleFor(x => x.AttributeName)
                .NotEmpty().WithMessage("Tên thuộc tính không được để trống.")
                .MaximumLength(255).WithMessage("Tên thuộc tính không được vượt quá 255 ký tự.");

            RuleFor(x => x.DataType)
                .IsInEnum().WithMessage("Kiểu dữ liệu (DataType) không hợp lệ.");

            RuleFor(x => x.InputMode)
                .NotNull().WithMessage("Chế độ nhập liệu (InputMode) không được để trống.")
                .IsInEnum().WithMessage("Chế độ nhập liệu (InputMode) không hợp lệ.");

            RuleFor(x => x.Unit)
                .MaximumLength(50).WithMessage("Đơn vị tính không được vượt quá 50 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue)
                .WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
        }
    }

    // Sub-validator dùng để kiểm tra chi tiết từng Option được truyền lên
    public class CreateAttributeOptionRequestValidator : AbstractValidator<CreateAttributeOptionRequest>
    {
        public CreateAttributeOptionRequestValidator()
        {
            RuleFor(x => x.OptionValue)
                .NotEmpty().WithMessage("Giá trị tùy chọn (OptionValue) không được để trống.")
                .MaximumLength(255).WithMessage("Giá trị tùy chọn không được vượt quá 255 ký tự.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue)
                .WithMessage("Thứ tự hiển thị tùy chọn phải lớn hơn hoặc bằng 0.");
        }
    }
}
