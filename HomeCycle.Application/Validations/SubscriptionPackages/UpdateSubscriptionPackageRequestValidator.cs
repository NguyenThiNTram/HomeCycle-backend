using FluentValidation;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.SubscriptionPackages
{
    public class UpdateSubscriptionPackageRequestValidator : AbstractValidator<UpdateSubscriptionPackageRequest>
    {
        public UpdateSubscriptionPackageRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Package name cannot be empty.")
                .MaximumLength(255)
                .WithMessage("Package name cannot exceed 255 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Package price must be greater than zero.")
                .Must(x => !x.HasValue || BeSupportedPaymentAmount(x.Value))
                .WithMessage("Package price must be a whole VND amount and cannot exceed the payment gateway limit.")
                .When(x => x.Price.HasValue);

            RuleFor(x => x.Duration)
                .InclusiveBetween(1, 3650)
                .WithMessage("Package duration must be between 1 and 3650 days.")
                .When(x => x.Duration.HasValue);

            RuleFor(x => x.Entitlements)
                .Must(x => x == null || x.Count > 0)
                .WithMessage("At least one entitlement is required when entitlements are updated.")
                .Must(HaveUniqueKeys)
                .WithMessage("Entitlement keys must be unique.");

            RuleForEach(x => x.Entitlements)
                .SetValidator(new PackageEntitlementRequestValidator())
                .When(x => x.Entitlements != null);
        }

        private static bool BeSupportedPaymentAmount(decimal price)
        {
            return decimal.Truncate(price) == price && price <= int.MaxValue;
        }
        private static bool HaveUniqueKeys(List<PackageEntitlementRequest>? entitlements)
        {
            if (entitlements == null)
                return true;

            var keys = entitlements
                .Select(x => x.Key?.Trim() ?? string.Empty)
                .ToList();

            return keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == keys.Count;
        }
    }
}
