using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.SubscriptionPackages
{
    public class EntitlementDefinitionResponseDto
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public EntitlementValueType ValueType { get; set; }
        public bool SupportsUnlimited { get; set; }
        public IReadOnlyCollection<UserRole> TargetRoles { get; set; } = Array.Empty<UserRole>();
    }
}
