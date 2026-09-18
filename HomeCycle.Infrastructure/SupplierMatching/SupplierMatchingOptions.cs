namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class SupplierMatchingOptions
{
    public int VipResultLimit { get; set; } = 20;
    public int CacheMinutes { get; set; } = 30;
    public int FallbackCacheMinutes { get; set; } = 3;
}
