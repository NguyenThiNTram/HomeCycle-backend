using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Banks;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeCycle.Application.Validations.Banks
{
    public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
    {
        public const string NameMismatchMessage = "Tên chủ tài khoản ngân hàng phải trùng với họ tên trên CCCD đã khai báo.";

        public UpdateBankAccountRequestValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(x => x.BankCode).NotEmpty().WithMessage("Vui lòng chọn ngân hàng.").MaximumLength(20);
            RuleFor(x => x.BankName).NotEmpty().WithMessage("Tên ngân hàng không được để trống.").MaximumLength(255);
            RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("Số tài khoản không được để trống.")
                .Matches(@"^[0-9A-Za-z]+$").WithMessage("Vui lòng nhập số tài khoản đầy đủ chỉ chứa chữ và số, không dùng số đã che.")
                .MaximumLength(50);
            RuleFor(x => x.AccountName).NotEmpty().WithMessage("Tên chủ tài khoản không được để trống.").MaximumLength(255);
        }

        public static bool NamesMatch(string? accountName, string? identityName)
        {
            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(identityName)) return false;
            return NormalizeName(accountName) == NormalizeName(identityName);
        }

        private static string NormalizeName(string value)
        {
            var normalized = value.Trim().ToUpperInvariant().Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var result = new StringBuilder();
            foreach (var character in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    result.Append(character);
            return Regex.Replace(result.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ");
        }
    }
}
