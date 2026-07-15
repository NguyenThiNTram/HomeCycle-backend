using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Auths
{
    public class SubmitBusinessProfileRequestValidator : AbstractValidator<SubmitBusinessProfileRequest>
    {
        public SubmitBusinessProfileRequestValidator()
        {
            // 1. Validate Representative Full Name (Cho phép Null, nhưng nếu điền thì giới hạn độ dài ký tự)
            RuleFor(x => x.FullName)
                .MaximumLength(255).WithMessage("Representative full name must not exceed 255 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.FullName));

            // 2. Validate Business Core Info
            RuleFor(x => x.BusinessName)
                .NotEmpty().WithMessage("Business name is required.")
                .MaximumLength(255).WithMessage("Business name must not exceed 255 characters.");

            RuleFor(x => x.TaxCode)
                .NotEmpty().WithMessage("Tax code is required.")
                .MaximumLength(50).WithMessage("Tax code must not exceed 50 characters.");

            // Chốt chặn bảo mật: Định danh CCCD đúng 12 chữ số theo luật định danh Việt Nam
            RuleFor(x => x.IdentityNumber)
                .NotEmpty().WithMessage("Identity card number (CCCD) is required.")
                .Matches(@"^[0-9]{12}$").WithMessage("Identity number must be exactly 12 numeric digits.");

            // 3. Validate Address (Nhận dữ liệu sạch đã bóc tách từ Frontend)
            RuleFor(x => x.BusinessAddress).NotEmpty().WithMessage("Business address is required.");
            RuleFor(x => x.Ward).NotEmpty().WithMessage("Ward is required.");
            RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");

            RuleFor(x => x.BusinessModel)
                .InclusiveBetween(0, 1).WithMessage("Invalid business model. (0: Household Business, 1: Enterprise).");

            // 4. Validate Bank Account Info (Phục vụ rút tiền tự động Payouts)
            RuleFor(x => x.BankCode).NotEmpty().WithMessage("Bank code is required.");
            RuleFor(x => x.BankName).NotEmpty().WithMessage("Bank name is required.");
            RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("Bank account number is required.");
            RuleFor(x => x.AccountName).NotEmpty().WithMessage("Bank account holder name is required.");

            // 5. Validate Registered Product Categories
            RuleFor(x => x.ProductTypeIds)
                .NotEmpty().WithMessage("At least one business product type category must be selected.");

            // 6. Chốt chặn tài liệu chứng thực upload bắt buộc (CCCD trước/sau + Giấy phép)
            RuleFor(x => x.Documents)
                .NotEmpty().WithMessage("Business documentation list is required.");

            RuleForEach(x => x.Documents).ChildRules(doc =>
            {
                doc.RuleFor(d => d.DocumentType).InclusiveBetween(0, 3).WithMessage("Invalid document type.");
                doc.RuleFor(d => d.DocumentUrl).NotEmpty().WithMessage("Document upload URL for this slot is required.");
            });

            RuleFor(x => x)
                .Must(x => x.Documents != null &&
                           x.Documents.Any(d => d.DocumentType == 0 && !string.IsNullOrWhiteSpace(d.DocumentUrl)) &&
                           x.Documents.Any(d => d.DocumentType == 1 && !string.IsNullOrWhiteSpace(d.DocumentUrl)) &&
                           x.Documents.Any(d => d.DocumentType == 2 && !string.IsNullOrWhiteSpace(d.DocumentUrl)))
                .WithMessage("Registration requires uploading all mandatory documents: CCCD Front, CCCD Back, and Business Registration Certificate.");

            // 7. Chốt chặn phân vùng kho bãi hoạt động (Chỉ ép buộc đối với Enterprise)
            RuleFor(x => x.ServiceAreas)
                .NotEmpty().WithMessage("Enterprises are required to register at least one warehouse/service area (Business Service Area).")
                .When(x => x.BusinessModel == 1);

            RuleForEach(x => x.ServiceAreas).ChildRules(area =>
            {
                area.RuleFor(a => a.City).NotEmpty().WithMessage("Warehouse City is required.");
                area.RuleFor(a => a.District).NotEmpty().WithMessage("Warehouse District is required.");
                area.RuleFor(a => a.Ward).NotEmpty().WithMessage("Warehouse Ward is required.");
            });
        }
    }
}
