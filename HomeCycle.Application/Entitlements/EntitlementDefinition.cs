using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Entitlements
{
    public sealed record EntitlementDefinition(
        string Key,
        string DisplayName,
        EntitlementValueType ValueType,
        bool SupportsUnlimited,
        IReadOnlyCollection<UserRole> TargetRoles);
}
