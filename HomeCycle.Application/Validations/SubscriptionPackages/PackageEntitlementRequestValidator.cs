using FluentValidation;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.SubscriptionPackages
{
    public class PackageEntitlementRequestValidator : AbstractValidator<PackageEntitlementRequest>
    {
        public PackageEntitlementRequestValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .WithMessage("Entitlement key is required.")
                .MaximumLength(100)
                .WithMessage("Entitlement key cannot exceed 100 characters.");
        }
    }
}
