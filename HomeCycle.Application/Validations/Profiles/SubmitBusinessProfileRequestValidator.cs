using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class SubmitBusinessProfileRequestValidator : AbstractValidator<SubmitBusinessProfileRequest>
    {
        public SubmitBusinessProfileRequestValidator()
        {
            // 1. Validate Representative Full Name (Cho phép Null, nhưng nếu điền thì giới hạn độ dài ký tự)
            RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Representative full name is required.")
            .MaximumLength(255).WithMessage("Representative full name must not exceed 255 characters.")
            .Must((request, fullName) => IsNameMatch(fullName, request.IdentityName))
            .WithMessage("FullName bắt buộc phải khớp chính xác với IdentityName (Tên trên CCCD).");

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

            RuleFor(x => x.IdentityName)
                .NotEmpty().WithMessage("Full name on Identity Card is required.")
                .MaximumLength(255).WithMessage("Identity name must not exceed 255 characters.");

            RuleFor(x => x.IdentityDob)
                .NotEmpty().WithMessage("Date of birth on Identity Card is required.")
                .Must(dob => dob != default(DateOnly)).WithMessage("Invalid date of birth.")
                .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.");

            RuleFor(x => x.IdentityAddress)
                .NotEmpty().WithMessage("Address on Identity Card is required.");

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

            // 6. Chốt chặn tài liệu chứng thực upload bắt buộc (CCCD trước/sau + Giấy phép)
            RuleFor(x => x)
            .Custom((request, context) =>
            {
                // Đọc cờ từ Service truyền vào qua RootContextData
                bool isResubmit = context.RootContextData.ContainsKey("IsResubmit") && (bool)context.RootContextData["IsResubmit"];

                // Lấy danh sách các DocumentType đã có bản active trong DB (Service truyền vào)
                var existingActiveTypes = context.RootContextData.ContainsKey("ExistingActiveDocTypes")
                    ? (List<int>)context.RootContextData["ExistingActiveDocTypes"]
                    : new List<int>();

                var uploadedTypes = request.Documents?.Where(d => d.DocumentUrl != null && d.DocumentUrl.Length > 0)
                                                     .Select(d => d.DocumentType).ToList() ?? new List<int>();

                // Các document type bắt buộc phải có (0: CccdFront, 1: CccdBack, 2: BusinessReg)
                int[] requiredTypes = { 0, 1, 2 };

                foreach (var reqType in requiredTypes)
                {
                    // Nếu user CÓ gửi file mới -> Pass
                    if (uploadedTypes.Contains(reqType)) continue;

                    // Nếu user KHÔNG gửi file mới, nhưng đây là Resubmit và DB đã có bản active -> Pass
                    if (isResubmit && existingActiveTypes.Contains(reqType)) continue;

                    // Còn lại -> Thiếu file, quăng lỗi
                    context.AddFailure("Documents", $"Bắt buộc phải đính kèm tài liệu loại {reqType} (CCCD mặt trước/CCCD mặt sau/Giấy ĐKKD).");
                }
            });

            // 7. Chốt chặn phân vùng kho bãi hoạt động (Chỉ ép buộc đối với Enterprise)
            RuleFor(x => x.ServiceArea)
                .NotEmpty().WithMessage("Enterprises are required to register at least one warehouse/service area (Business Service Area).")
                .When(x => x.BusinessModel == 1);

            When(x => x.ServiceArea != null, () =>
            {
                RuleFor(x => x.ServiceArea!.City).NotEmpty().WithMessage("Warehouse City is required.");
                RuleFor(x => x.ServiceArea!.Street).NotEmpty().WithMessage("Warehouse Street/Address is required.");
                RuleFor(x => x.ServiceArea!.Ward).NotEmpty().WithMessage("Warehouse Ward is required.");
            });
        }

        private bool IsNameMatch(string name1, string name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2)) return false;

            return NormalizeString(name1) == NormalizeString(name2);
        }

        private string NormalizeString(string input)
        {
            // 1. Chuyển Unicode tổ hợp về dựng sẵn (FormC)
            var normalized = input.Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim();
            // 2. Xóa các khoảng trắng thừa ở giữa
            return Regex.Replace(normalized, @"\s+", " ");
        }
    }
}
