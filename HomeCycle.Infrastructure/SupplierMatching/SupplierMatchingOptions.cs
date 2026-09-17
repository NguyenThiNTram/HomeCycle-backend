namespace HomeCycle.Infrastructure.SupplierMatching;

public sealed class SupplierMatchingOptions
{
    public int FreeResultLimit { get; set; } = 5;
    public int VipResultLimit { get; set; } = 20;
    public int FreeDailyAiRefreshLimit { get; set; } = 3;
    public int VipDailyAiRefreshLimit { get; set; } = 25;
    public int CacheMinutes { get; set; } = 30;
    public int[] ActiveSubscriptionStatuses { get; set; } = [1];
    public string[] VipPackageCodes { get; set; } =
        ["VIP", "PREMIUM", "BUSINESS", "CAOCAP", "DOANHNGHIEP"];
}
