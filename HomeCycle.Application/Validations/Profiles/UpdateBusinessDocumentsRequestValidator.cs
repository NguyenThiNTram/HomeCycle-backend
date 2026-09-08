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
               .Cascade(CascadeMode.Stop)
               .NotNull()
               .WithMessage("Danh sách tài liệu không được null.")
               .NotEmpty()
               .WithMessage(
                   "Cần cung cấp ít nhất một tài liệu đính kèm.");

            RuleForEach(x => x.Documents).ChildRules(document =>
            {
                document.RuleFor(x => x.DocumentType)
                    .InclusiveBetween(0, 3)
                    .WithMessage("Loại tài liệu không hợp lệ.");

                document.RuleFor(x => x.DocumentUrl)
                    .NotNull()
                    .WithMessage("File tài liệu không được để trống.");
            });

            //RuleFor(x => x.Documents)
            //    .Must(documents =>
            //    {
            //        if (documents == null)
            //            return true;

            //        return documents
            //            .Select(x => x.DocumentType)
            //            .Distinct()
            //            .Count() == documents.Count;
            //    })
            //    .WithMessage("Mỗi loại tài liệu chỉ được gửi lên một lần.");

            //RuleForEach(x => x.Documents).ChildRules(doc =>
            //{
            //    doc.RuleFor(d => d.DocumentType)
            //        .InclusiveBetween(0, 3).WithMessage("Loại tài liệu không hợp lệ.");

            //    doc.RuleFor(d => d.DocumentUrl)
            //        .NotNull().WithMessage("File tài liệu không được để trống.")
            //        .Must(f => f != null && f.Length > 0).WithMessage("File tài liệu không hợp lệ.");
            //});
        }
    }
}
