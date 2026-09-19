namespace HomeCycle.Application.Entitlements;

public sealed class FreePlanOptions
{
    public FreePersonalPlanOptions Personal { get; set; } = new();
    public FreeBusinessPlanOptions Business { get; set; } = new();
}

public sealed class FreePersonalPlanOptions
{
    public string Name { get; set; } = "Gói Miễn phí";
    public string Description { get; set; } = "Sử dụng tính năng gợi ý giá theo hạn mức miễn phí mỗi ngày.";
    public int PriceSuggestionDailyLimit { get; set; } = 5;
}

public sealed class FreeBusinessPlanOptions
{
    public string Name { get; set; } = "Gói Miễn phí";
    public string Description { get; set; } = "Tìm nhà cung cấp bằng matching cơ bản và AI theo hạn mức miễn phí mỗi ngày.";
    public int SupplierMatchDailyLimit { get; set; } = 5;
    public int SupplierMatchResultLimit { get; set; } = 5;
    public bool AiRerankingEnabled { get; set; } = true;
    public bool AdvancedFiltersEnabled { get; set; }
    public bool DetailedReasonsEnabled { get; set; }
    public bool NewSupplierNotificationsEnabled { get; set; }
}
