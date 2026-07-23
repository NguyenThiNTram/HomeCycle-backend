using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Posts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Posts
{
    public class PostSearchRequestValidator : AbstractValidator<PostSearchRequest>
    {
        public PostSearchRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải trong khoảng 1-100.");

            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("Giá tối thiểu không thể lớn hơn giá tối đa.")
                .OverridePropertyName("PriceRange");

            RuleFor(x => x)
                .Must(x => !x.MinUsageDuration.HasValue || !x.MaxUsageDuration.HasValue || x.MinUsageDuration <= x.MaxUsageDuration)
                .WithMessage("Thời gian sử dụng tối thiểu không thể lớn hơn tối đa.")
                .OverridePropertyName("UsageDurationRange");

            RuleFor(x => x)
                .Must(x => !x.MinDamageLevel.HasValue || !x.MaxDamageLevel.HasValue || x.MinDamageLevel <= x.MaxDamageLevel)
                .WithMessage("Mức độ hư hỏng tối thiểu không thể lớn hơn tối đa.")
                .OverridePropertyName("DamageLevelRange");

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
                .WithMessage("Giá tối thiểu không được âm.");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
                .WithMessage("Giá tối đa không được âm.");

            RuleFor(x => x.PostedWithinDays)
                .GreaterThan(0).When(x => x.PostedWithinDays.HasValue)
                .WithMessage("Số ngày phải lớn hơn 0.");

            // Cả ProductType lẫn Brand đều bắt buộc đi kèm CategoryId đã chọn trước
            RuleFor(x => x.CategoryId)
                .NotNull().When(x => x.ProductTypeId.HasValue)
                .WithMessage("Cần chọn danh mục (Category) trước khi lọc theo loại sản phẩm.");

            RuleFor(x => x.CategoryId)
                .NotNull().When(x => x.BrandId.HasValue)
                .WithMessage("Cần chọn danh mục (Category) trước khi lọc theo thương hiệu.");

            RuleForEach(x => x.AttributeFilters)
                .SetValidator(new AttributeFilterRequestValidator());
    }
}

    public class AttributeFilterRequestValidator : AbstractValidator<AttributeFilterRequest>
    {
        public AttributeFilterRequestValidator()
        {
            RuleFor(x => x.AttributeId)
                .NotEmpty().WithMessage("AttributeId không được để trống.");

            // Mỗi filter phải có ĐÚNG 1 loại điều kiện được điền — tránh gửi lẫn lộn nhiều kiểu
            // (VD: vừa OptionIds vừa MinValue) gây khó hiểu khi Repository build query.
            RuleFor(x => x)
                .Must(HaveExactlyOneConditionType)
                .WithMessage("Mỗi bộ lọc thuộc tính chỉ được chọn đúng 1 kiểu điều kiện: Option, Khoảng số, Boolean, hoặc Văn bản.");

            RuleFor(x => x)
                .Must(x => !x.MinValue.HasValue || !x.MaxValue.HasValue || x.MinValue <= x.MaxValue)
                .WithMessage("Giá trị tối thiểu không thể lớn hơn giá trị tối đa.")
                .OverridePropertyName("ValueRange");
        }

        private static bool HaveExactlyOneConditionType(AttributeFilterRequest x)
        {
            int count = 0;
            if (x.OptionIds is { Count: > 0 }) count++;
            if (x.MinValue.HasValue || x.MaxValue.HasValue) count++;
            if (x.ValueBoolean.HasValue) count++;
            if (!string.IsNullOrWhiteSpace(x.ValueTextContains)) count++;
            return count == 1;
        }
    }
}
