using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Validations.Files;
using HomeCycle.Application.Validations.Products;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Posts
{
    public class CreateSellPostRequestValidator : AbstractValidator<CreateSellPostRequest>
    {
        private readonly IProductAttributeRepository _attributeRepository;
        private readonly IProductAttributeOptionRepository _optionRepository;

        public CreateSellPostRequestValidator(
            IProductAttributeRepository attributeRepository,
            IProductAttributeOptionRepository optionRepository)
        {
            _attributeRepository = attributeRepository;
            _optionRepository = optionRepository;

            // Kế thừa các rule chung từ CreatePostRequestValidator
            Include(new CreatePostRequestValidator());

            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải lớn hơn hoặc bằng 0.");

            // SRS: Upload hình ảnh sản phẩm bắt buộc cho bài đăng bán
            RuleFor(x => x.Medias)
                .NotNull().WithMessage("Danh sách hình ảnh không được null.")
                .Must(m => m != null && m.Count > 0)
                .WithMessage("Bài đăng bán bắt buộc phải có ít nhất 1 hình ảnh sản phẩm.");

            RuleForEach(x => x.Medias)
                .SetValidator(new FormFileValidator());

            RuleFor(x => x.Product)
                .NotNull().WithMessage("Thông tin sản phẩm không được để trống.")
                .SetValidator(new ProductRequestValidator());

            // Validate các thuộc tính động của Product từ Database
            RuleFor(x => x).CustomAsync(ValidateAttributesWithDatabaseAsync);
        }

        private async Task ValidateAttributesWithDatabaseAsync(
            CreateSellPostRequest request,
            ValidationContext<CreateSellPostRequest> context,
            CancellationToken token)
        {
            if (request.Product is null) return;

            var productTypeId = request.Product.ProductTypeId;
            var attributeRequests = request.Product.AttributeValues ?? new List<ProductAttributeValueRequest>();

            // A. Kiểm tra trùng lặp AttributeId trong cùng 1 Request
            var duplicateIds = attributeRequests
                .GroupBy(x => x.AttributeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var dupId in duplicateIds)
            {
                context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dupId}' bị gửi lặp lại nhiều lần.");
            }

            var definedAttributes = await _attributeRepository.GetByProductTypeAsync(productTypeId);
            var definedAttributeMap = definedAttributes.ToDictionary(x => x.AttributeId);

            foreach (var attrReq in attributeRequests)
            {
                if (!definedAttributeMap.TryGetValue(attrReq.AttributeId, out var dbAttr))
                {
                    context.AddFailure("Product.AttributeValues", $"Thuộc tính '{attrReq.AttributeId}' không tồn tại hoặc không thuộc loại sản phẩm này.");
                    continue;
                }

                var inputMode = dbAttr.InputMode;
                var dataType = (DataType)(dbAttr.DataType ?? 0);

                bool hasOption = attrReq.OptionId.HasValue;
                bool hasCustom = attrReq.ValueBoolean.HasValue
                              || attrReq.ValueNumber.HasValue
                              || !string.IsNullOrWhiteSpace(attrReq.ValueText);

                // 1. Kiểm tra Option thuộc Attribute nếu client có chọn Option
                if (hasOption)
                {
                    var option = await _optionRepository.GetByIdAsync(attrReq.OptionId.Value);
                    if (option is null || option.AttributeId != dbAttr.AttributeId)
                    {
                        context.AddFailure("Product.AttributeValues", $"Tùy chọn đã chọn không hợp lệ cho thuộc tính '{dbAttr.AttributeName}'.");
                    }
                }

                // 2. Kiểm tra ràng buộc theo InputMode
                switch (inputMode)
                {
                    case InputMode.OptionOnly:
                        if (!hasOption)
                            context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dbAttr.AttributeName}' chỉ được chọn từ danh sách tùy chọn, không nhập tay.");
                        else if (hasCustom)
                            context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dbAttr.AttributeName}' không được phép nhập giá trị tự do khi chọn từ danh sách.");
                        break;

                    case InputMode.CustomOnly:
                        if (hasOption)
                            context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dbAttr.AttributeName}' không cho phép chọn tùy chọn, chỉ được nhập tay.");
                        else if (!hasCustom)
                            context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dbAttr.AttributeName}' phải nhập giá trị.");
                        break;

                    case InputMode.OptionOrCustom:
                        if (hasOption && hasCustom)
                            context.AddFailure("Product.AttributeValues", $"Thuộc tính '{dbAttr.AttributeName}' chỉ được điền 1 trong 2: chọn tùy chọn HOẶC nhập tay.");
                        break;
                }

                // 3. Kiểm tra giá trị nhập tay khớp DataType (Text/Number/Boolean)
                if (hasCustom)
                {
                    bool matchesDataType = (dataType == DataType.Text && !string.IsNullOrWhiteSpace(attrReq.ValueText))
                        || (dataType == DataType.Number && attrReq.ValueNumber.HasValue)
                        || (dataType == DataType.Boolean && attrReq.ValueBoolean.HasValue);

                    if (!matchesDataType)
                        context.AddFailure("Product.AttributeValues", $"Giá trị nhập tay của thuộc tính '{dbAttr.AttributeName}' không đúng kiểu dữ liệu ({dataType}).");
                }
            }

            var submittedAttributeIds = attributeRequests.Select(x => x.AttributeId).ToHashSet();
            var missingRequiredAttributes = definedAttributes
                .Where(x => x.IsRequired && !submittedAttributeIds.Contains(x.AttributeId))
                .ToList();

            foreach (var missingAttr in missingRequiredAttributes)
            {
                context.AddFailure("Product.AttributeValues", $"Thuộc tính bắt buộc '{missingAttr.AttributeName}' chưa được điền.");
            }
        }
    }

    public class UpdateSellPostRequestValidator : AbstractValidator<UpdateSellPostRequest>
    {
        public UpdateSellPostRequestValidator(
            IProductAttributeRepository attributeRepository,
            IProductAttributeOptionRepository optionRepository)
        {
            Include(new CreatePostRequestValidator());

            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải lớn hơn hoặc bằng 0.");

            RuleFor(x => x.Product)
                .NotNull().WithMessage("Thông tin sản phẩm không được để trống.")
                .SetValidator(new ProductRequestValidator());

            RuleForEach(x => x.Medias)
                .SetValidator(new FormFileValidator());
        }
    }
}
