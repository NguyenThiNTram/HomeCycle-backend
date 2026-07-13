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
                .NotEmpty().WithMessage("The avatar field cannot be left blank.")
                .Must(BeAValidUrl).WithMessage("Invalid avatar URL.");
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
                !string.IsNullOrWhiteSpace(x.FrontIDCardImage) ||
                !string.IsNullOrWhiteSpace(x.BackIDCardImage),
                () =>
                {
                    RuleFor(x => x.RepresentativeCode)
                        .NotEmpty()
                        .MaximumLength(50);

                    RuleFor(x => x.RepresentativeName)
                        .NotEmpty()
                        .MaximumLength(255);

                    RuleFor(x => x.RepresentativeAddress)
                        .NotEmpty()
                        .MaximumLength(500);

                    RuleFor(x => x.FrontIDCardImage)
                        .NotEmpty()
                        .MaximumLength(1000);

                    RuleFor(x => x.BackIDCardImage)
                        .NotEmpty()
                        .MaximumLength(1000);
                });
        }
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
