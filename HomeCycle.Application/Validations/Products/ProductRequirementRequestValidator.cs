using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Products
{
    public class ProductRequirementRequestValidator : AbstractValidator<ProductRequirementRequest>
    {
        public ProductRequirementRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty()
                .WithMessage("Mã danh mục (CategoryId) không được để trống.");

            RuleFor(x => x.ProductTypeId)
                .NotEmpty()
                .WithMessage("Mã loại sản phẩm (ProductTypeId) không được để trống.");

            RuleFor(x => x.BrandId)
                .NotEmpty()
                .When(x => x.BrandId.HasValue)
                .WithMessage("Mã thương hiệu (BrandId) không hợp lệ.");

            RuleFor(x => x.ExpectedPrice)
                .GreaterThanOrEqualTo(0)
                .When(x => x.ExpectedPrice.HasValue)
                .WithMessage("Mức giá mong muốn phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.ProductName)
                .MaximumLength(255)
                .WithMessage("Tên sản phẩm mong muốn không được vượt quá 255 ký tự.");

            RuleFor(x => x.SpaceUsage)
                .MaximumLength(255)
                .WithMessage("Mô tả không gian sử dụng không được vượt quá 255 ký tự.");

            RuleFor(x => x.FunctionalityStatus)
                .IsInEnum()
                .When(x => x.FunctionalityStatus.HasValue)
                .WithMessage("Tình trạng hoạt động (FunctionalityStatus) không hợp lệ.");

            RuleFor(x => x.UsageDuration)
                .GreaterThanOrEqualTo(0)
                .When(x => x.UsageDuration.HasValue)
                .WithMessage("Thời gian đã sử dụng phải lớn hơn hoặc bằng 0 tháng.");

            RuleFor(x => x.DamageLevel)
                .InclusiveBetween(0, 100)
                .When(x => x.DamageLevel.HasValue)
                .WithMessage("Mức độ hư hại (DamageLevel) phải nằm trong khoảng từ 0% đến 100%.");

            // Tái sử dụng Validator cho danh sách các thuộc tính sản phẩm đính kèm
            RuleForEach(x => x.AttributeValues)
                .SetValidator(new ProductAttributeValueRequestValidator());
        }
    }
}
