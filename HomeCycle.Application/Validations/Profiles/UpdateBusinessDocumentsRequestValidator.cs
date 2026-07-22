using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class UpdateBusinessDocumentsRequestValidator : AbstractValidator<UpdateBusinessDocumentsRequest>
    {
        public UpdateBusinessDocumentsRequestValidator()
        {
            RuleFor(x => x.Documents)
                .NotNull().WithMessage("Danh sách tài liệu không được null.")
                .Must(docs => docs != null && docs.Count > 0).WithMessage("Cần cung cấp ít nhất một tài liệu đính kèm.");

            RuleForEach(x => x.Documents).ChildRules(doc =>
            {
                doc.RuleFor(d => d.DocumentType)
                    .GreaterThan(0).WithMessage("Loại tài liệu không hợp lệ.");

                doc.RuleFor(d => d.DocumentUrl)
                    .NotEmpty().WithMessage("Đường dẫn tài liệu không được để trống.")
                    .MaximumLength(500).WithMessage("Đường dẫn tài liệu quá dài.");
            });
        }
    }
}
