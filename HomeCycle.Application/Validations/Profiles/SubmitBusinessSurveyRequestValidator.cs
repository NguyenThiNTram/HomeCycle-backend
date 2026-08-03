using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Profiles
{
    public class SubmitBusinessSurveyRequestValidator : AbstractValidator<SubmitBusinessSurveyRequest>
    {
        public SubmitBusinessSurveyRequestValidator()
        {
            RuleFor(x => x.TargetCities).NotEmpty().WithMessage("Danh sách thành phố không được để trống.");
            RuleFor(x => x.AcceptableDamageLevels).NotEmpty().WithMessage("Mức độ hư hại không được để trống.");
            RuleFor(x => x.AcceptableFunctionalityStatuses).NotEmpty().WithMessage("Tình trạng hoạt động không được để trống.");
            RuleFor(x => x.ProcurementScales).NotEmpty().WithMessage("Quy mô thu mua không được để trống.");
            RuleFor(x => x.ProductTypeIds).NotEmpty().WithMessage("Danh mục loại sản phẩm không được để trống.");
        }
    }
}
