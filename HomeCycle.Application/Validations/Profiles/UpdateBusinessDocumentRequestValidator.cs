using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateBusinessDocumentRequestValidator : AbstractValidator<UpdateBusinessDocumentRequest>
    {
        public UpdateBusinessDocumentRequestValidator()
        {
            RuleFor(x => x.DocumentType)
                .GreaterThan(0).WithMessage("Loại tài liệu không hợp lệ.");

            RuleFor(x => x.DocumentUrl)
                .NotEmpty().WithMessage("Đường dẫn tài liệu không được để trống.")
                .MaximumLength(500).WithMessage("URL không vượt quá 500 ký tự.")
                .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Đường dẫn tài liệu phải là URL hợp lệ.");
        }
    }
}
