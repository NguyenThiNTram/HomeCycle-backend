using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Entitlements
{
    public static class EntitlementCatalog
    {
        public static IReadOnlyList<EntitlementDefinition> All { get; } =
        [
            new(EntitlementKeys.PriceSuggestionDailyCount, "Lượt AI gợi ý giá mỗi ngày", EntitlementValueType.Integer, false, new[] { UserRole.Personal }),
            new(EntitlementKeys.SupplierMatchDailyCount, "Lượt AI gợi ý nhà cung cấp mỗi ngày", EntitlementValueType.Integer, false, new[] { UserRole.Business }),
            new(
                EntitlementKeys.WithdrawalDailyCount,
                "Số lần rút tiền tối đa mỗi ngày",
                EntitlementValueType.Integer,
                true,
                new[] { UserRole.Business }),
            new(
                EntitlementKeys.WithdrawalDailyAmount,
                "Tổng số tiền được rút tối đa mỗi ngày",
                EntitlementValueType.Decimal,
                true,
                new[] { UserRole.Business })
        ];

        public static bool TryGet(string key, out EntitlementDefinition? definition)
        {
            definition = All.FirstOrDefault(x =>
                string.Equals(x.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

            return definition != null;
        }
    }
}
