namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class SupplierMatchingOptions
{
    public int FreeResultLimit { get; set; } = 5;
    public int VipResultLimit { get; set; } = 20;
    public int FreeDailyAiRefreshLimit { get; set; } = 5;
    public int VipDailyAiRefreshLimit { get; set; } = 100;
    public int CacheMinutes { get; set; } = 30;
    public int FallbackCacheMinutes { get; set; } = 3;
}
