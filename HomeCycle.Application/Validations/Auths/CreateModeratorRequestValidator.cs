using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Auths;

namespace HomeCycle.Application.Validations.Auths;

public sealed class CreateModeratorRequestValidator : AbstractValidator<CreateModeratorRequest>
{
    public CreateModeratorRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100).Matches(@"\A[a-zA-Z0-9_]+\z");
    }
}
