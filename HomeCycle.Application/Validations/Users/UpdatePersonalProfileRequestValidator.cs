using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Users
{
    public class UpdatePersonalProfileRequestValidator : AbstractValidator<UpdatePersonalProfileRequest>
    {
        public UpdatePersonalProfileRequestValidator()
        {
            RuleFor(x => x.Username)
                .MaximumLength(100).WithMessage("The username must not exceed 100 characters.");

            RuleFor(x => x.FullName)
                .MaximumLength(255).WithMessage("The full name must not exceed 255 characters.");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("The address must not exceed 500 characters.");
            
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("Invalid phone number.");
        }
    }

    public class UpdateAvatarRequestValidator : AbstractValidator<UpdateAvatarRequest>
    {
        public UpdateAvatarRequestValidator()
        {
            RuleFor(x => x.AvatarUrl)
                .NotEmpty().WithMessage("The avatar field cannot be left blank.");
                //.Must(BeAValidUrl).WithMessage("Invalid avatar URL.");
        }

        private bool BeAValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public class UpdateIdCardRequestValidator : AbstractValidator<UpdateIdCardRequest>
    {
        public UpdateIdCardRequestValidator()
        {
            // Nếu bất kỳ field CCCD được nhập, thì yêu cầu RepresentativeCode và RepresentativeName không rỗng
            When(x =>
            !string.IsNullOrWhiteSpace(x.RepresentativeCode) ||
            !string.IsNullOrWhiteSpace(x.RepresentativeName) ||
            x.RepresentativeDob.HasValue ||
            !string.IsNullOrWhiteSpace(x.RepresentativeAddress) ||
            (x.FrontIDCardImage != null && x.FrontIDCardImage.Length > 0) || // ĐÃ SỬA: Kiểm tra file hợp lệ
            (x.BackIDCardImage != null && x.BackIDCardImage.Length > 0),    // ĐÃ SỬA: Kiểm tra file hợp lệ
            () =>
            {
                RuleFor(x => x.RepresentativeCode)
                    .NotEmpty().WithMessage("Representative code is required.")
                    .MaximumLength(50).WithMessage("Representative code must not exceed 50 characters.");

                RuleFor(x => x.RepresentativeName)
                    .NotEmpty().WithMessage("Representative name is required.")
                    .MaximumLength(255).WithMessage("Representative name must not exceed 255 characters.");

                RuleFor(x => x.RepresentativeAddress)
                    .NotEmpty().WithMessage("Representative address is required.")
                    .MaximumLength(500).WithMessage("Representative address must not exceed 500 characters.");

                // ĐÃ SỬA: Thay thế quy tắc chuỗi thành quy tắc kiểm tra File ảnh bắt buộc
                RuleFor(x => x.FrontIDCardImage)
                    .NotNull().WithMessage("Front ID Card Image file is required.");

                RuleFor(x => x.BackIDCardImage)
                    .NotNull().WithMessage("Back ID Card Image file is required.");
            });
        }
    }

    public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequest>
    {
        public UpdateBankAccountRequestValidator()
        {
            When(x =>
                !string.IsNullOrWhiteSpace(x.BankCode) ||
                !string.IsNullOrWhiteSpace(x.BankName) ||
                !string.IsNullOrWhiteSpace(x.AccountNumber) ||
                !string.IsNullOrWhiteSpace(x.AccountName),
                () =>
                {
                    RuleFor(x => x.BankCode)
                        .NotEmpty()
                        .MaximumLength(50);

                    RuleFor(x => x.BankName)
                        .NotEmpty()
                        .MaximumLength(255);

                    RuleFor(x => x.AccountNumber)
                        .NotEmpty()
                        .MaximumLength(50);

                    RuleFor(x => x.AccountName)
                        .NotEmpty()
                        .MaximumLength(255);
                });
        }
    }
}
