namespace HomeCycle.Application.Pricing.Models;

// Cấu hình gợi ý giá, đọc từ section "PriceSuggestion" trong appsettings.
public sealed class PriceSuggestionOptions
{
    public const string SectionName = "PriceSuggestion";

    // Dùng giao dịch và tin đăng trên HomeCycle làm mẫu giá máy cũ. Đang tắt vì dữ liệu hiện tại
    // là dữ liệu demo của đồ án, giá thấp hơn thị trường. Bật lại khi đã có giao dịch thật.
    public bool UseHomeCyclePriceEvidence { get; set; }
}
