using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Auths;

namespace HomeCycle.Application.Validations.Auths;

public sealed class SetModeratorPasswordRequestValidator : AbstractValidator<SetModeratorPasswordRequest>
{
    public SetModeratorPasswordRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Token).NotEmpty().Matches(@"\A[0-9A-Fa-f]{64}\z");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(50);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password);
    }
}
