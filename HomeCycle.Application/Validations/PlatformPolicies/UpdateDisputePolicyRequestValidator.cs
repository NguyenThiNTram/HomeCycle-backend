using FluentValidation;
using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Application.DTOs.Requests.PlatformPolicies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.PlatformPolicies
{
    public class UpdateDisputePolicyRequestValidator : AbstractValidator<UpdateDisputePolicyRequest>
    {
        public UpdateDisputePolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x =>
                    x.NormalDisputeWindowDays.HasValue ||
                    x.LowReputationDisputeWindowDays.HasValue ||
                    x.LowReputationThreshold.HasValue ||
                    x.ReturnWindowDays.HasValue ||
                    x.DisputeLossPenaltyPoints.HasValue ||
                    x.PostViolationPenaltyPoints.HasValue ||
                    x.ReviewViolationPenaltyPoints.HasValue)
                .WithMessage("Phải cung cấp ít nhất một cấu hình cần thay đổi.");

            RuleFor(x => x.NormalDisputeWindowDays)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 365)
                .WithMessage("NormalDisputeWindowDays phải từ 1 đến 365 ngày.");

            RuleFor(x => x.LowReputationDisputeWindowDays)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 365)
                .WithMessage("LowReputationDisputeWindowDays phải từ 1 đến 365 ngày.");

            RuleFor(x => x.LowReputationThreshold)
                .Must(x => !x.HasValue || x.Value is >= 0 and <= 100)
                .WithMessage("LowReputationThreshold phải từ 0 đến 100.");

            RuleFor(x => x.ReturnWindowDays)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 30)
                .WithMessage("ReturnWindowDays phải từ 1 đến 30 ngày.");

            RuleFor(x => x.DisputeLossPenaltyPoints)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 100)
                .WithMessage("DisputeLossPenaltyPoints phải từ 1 đến 100.");

            RuleFor(x => x.PostViolationPenaltyPoints)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 100)
                .WithMessage("PostViolationPenaltyPoints phải từ 1 đến 100.");
            RuleFor(x => x.ReviewViolationPenaltyPoints)
                .Must(x => !x.HasValue || x.Value is >= 1 and <= 100)
                .WithMessage("ReviewViolationPenaltyPoints phải từ 1 đến 100.");
        }
    }

    public class UpdateRatingPolicyRequestValidator : AbstractValidator<UpdateRatingPolicyRequest>
    {
        public UpdateRatingPolicyRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => x.PriorMean.HasValue || x.PriorWeight.HasValue ||
                    x.FiveStarPoints.HasValue || x.FourStarPoints.HasValue ||
                    x.ThreeStarPoints.HasValue || x.TwoStarPoints.HasValue ||
                    x.OneStarPoints.HasValue || x.MinimumReputationScore.HasValue ||
                    x.MaximumReputationScore.HasValue || x.ReviewEditWindowDays.HasValue)
                .WithMessage("Cần cung cấp ít nhất một cấu hình rating để cập nhật.");

            RuleFor(x => x.PriorMean)
                .InclusiveBetween(1, 5)
                .When(x => x.PriorMean.HasValue);
            RuleFor(x => x.PriorWeight)
                .InclusiveBetween(0, 100)
                .When(x => x.PriorWeight.HasValue);
            RuleFor(x => x.FiveStarPoints)
                .InclusiveBetween(-20, 10)
                .When(x => x.FiveStarPoints.HasValue);
            RuleFor(x => x.FourStarPoints)
                .InclusiveBetween(-20, 10)
                .When(x => x.FourStarPoints.HasValue);
            RuleFor(x => x.ThreeStarPoints)
                .InclusiveBetween(-20, 10)
                .When(x => x.ThreeStarPoints.HasValue);
            RuleFor(x => x.TwoStarPoints)
                .InclusiveBetween(-20, 10)
                .When(x => x.TwoStarPoints.HasValue);
            RuleFor(x => x.OneStarPoints)
                .InclusiveBetween(-20, 10)
                .When(x => x.OneStarPoints.HasValue);
            RuleFor(x => x.MinimumReputationScore)
                .InclusiveBetween(0, 99)
                .When(x => x.MinimumReputationScore.HasValue);
            RuleFor(x => x.MaximumReputationScore)
                .InclusiveBetween(1, 100)
                .When(x => x.MaximumReputationScore.HasValue);
            RuleFor(x => x.ReviewEditWindowDays)
                .InclusiveBetween(1, 30)
                .When(x => x.ReviewEditWindowDays.HasValue);
        }
    }

    public class RatingPolicyConfigDtoValidator : AbstractValidator<RatingPolicyConfigDto>
    {
        public RatingPolicyConfigDtoValidator()
        {
            RuleFor(x => x.PriorMean).InclusiveBetween(1, 5);
            RuleFor(x => x.PriorWeight).InclusiveBetween(0, 100);
            RuleFor(x => x.FiveStarPoints).InclusiveBetween(-20, 10);
            RuleFor(x => x.FourStarPoints).InclusiveBetween(-20, 10);
            RuleFor(x => x.ThreeStarPoints).InclusiveBetween(-20, 10);
            RuleFor(x => x.TwoStarPoints).InclusiveBetween(-20, 10);
            RuleFor(x => x.OneStarPoints).InclusiveBetween(-20, 10);
            RuleFor(x => x.MinimumReputationScore).InclusiveBetween(0, 99);
            RuleFor(x => x.MaximumReputationScore).InclusiveBetween(1, 100);
            RuleFor(x => x.ReviewEditWindowDays).InclusiveBetween(1, 30);
            RuleFor(x => x).Must(x => x.MinimumReputationScore < x.MaximumReputationScore);
            RuleFor(x => x).Must(x =>
                x.FiveStarPoints >= x.FourStarPoints &&
                x.FourStarPoints >= x.ThreeStarPoints &&
                x.ThreeStarPoints >= x.TwoStarPoints &&
                x.TwoStarPoints >= x.OneStarPoints);
        }
    }
}
