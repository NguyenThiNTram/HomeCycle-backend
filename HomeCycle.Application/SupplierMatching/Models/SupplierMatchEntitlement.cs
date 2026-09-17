namespace HomeCycle.Application.SupplierMatching.Models;

public enum SupplierMatchTier
{
    Free = 0,
    Vip = 1
}

public sealed record SupplierMatchEntitlement(
    SupplierMatchTier Tier,
    int ResultLimit,
    int DailyAiRefreshLimit,
    bool AiRerankingEnabled,
    bool AdvancedFiltersEnabled,
    bool DetailedReasonsEnabled)
{
    public static SupplierMatchEntitlement Free(int resultLimit = 5) =>
        new(SupplierMatchTier.Free, resultLimit, 3, true, false, false);
}
