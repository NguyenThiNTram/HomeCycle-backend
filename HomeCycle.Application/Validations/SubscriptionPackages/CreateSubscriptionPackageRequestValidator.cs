using FluentValidation;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.SubscriptionPackages
{
    public class CreateSubscriptionPackageRequestValidator : AbstractValidator<CreateSubscriptionPackageRequest>
    {
        public CreateSubscriptionPackageRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Package code is required.")
                .MaximumLength(100)
                .WithMessage("Package code cannot exceed 100 characters.")
                .Matches("^[A-Za-z0-9_-]+$")
                .WithMessage("Package code may only contain letters, numbers, underscores and hyphens.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Package name is required.")
                .MaximumLength(255)
                .WithMessage("Package name cannot exceed 255 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Package price must be greater than zero.")
                .Must(BeSupportedPaymentAmount)
                .WithMessage("Package price must be a whole VND amount and cannot exceed the payment gateway limit.");

            RuleFor(x => x.Duration)
                .GreaterThan(0)
                .WithMessage("Package duration must be greater than zero.");

            RuleFor(x => x.TargetRole)
                .Equal(UserRole.Business)
                .WithMessage("Current subscription entitlements only support Business packages.");

            RuleFor(x => x.Entitlements)
                .NotEmpty()
                .WithMessage("At least one entitlement is required.")
                .Must(HaveUniqueKeys)
                .WithMessage("Entitlement keys must be unique.");

            RuleForEach(x => x.Entitlements)
                .SetValidator(new PackageEntitlementRequestValidator());
        }

        private static bool BeSupportedPaymentAmount(decimal price)
        {
            return decimal.Truncate(price) == price && price <= int.MaxValue;
        }
        private static bool HaveUniqueKeys(IEnumerable<PackageEntitlementRequest> entitlements)
        {
            var keys = entitlements
                .Select(x => x.Key?.Trim() ?? string.Empty)
                .ToList();

            return keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == keys.Count;
        }
    }
}
