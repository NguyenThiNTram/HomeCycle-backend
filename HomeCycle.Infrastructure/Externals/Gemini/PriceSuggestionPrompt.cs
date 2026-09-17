using System.Text.Json;
using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Infrastructure.Externals.Gemini;

internal static class PriceSuggestionPrompt
{
    private const string Instructions = """
        Bạn đề xuất giá bán đồ cũ theo VND cho một sản phẩm.
        Chỉ sử dụng dữ liệu JSON được cung cấp. Không tìm kiếm thêm và không tạo URL.
        Dữ liệu đúng model được ưu tiên tuyệt đối.
        Chỉ dùng nhóm model tương đương khi context không có giá đồ cũ đúng model.
        Không tự áp dụng tỷ lệ tăng hoặc giảm giá giữa các model.
        Khi dùng model tương đương, phải trả reason code EQUIVALENT_MODEL_REFERENCE.
        Giao dịch hoàn thành là chứng cứ chính. Giá bài đăng là chứng cứ tham khảo và giá sản phẩm mới chỉ là ngữ cảnh.
        Giá mới không được dùng một mình để suy ra giá đồ cũ.
        suggestedPrice, minPrice và maxPrice phải nằm hoàn toàn trong allowedPriceRange.
        Nếu dữ liệu hạn chế, dùng reason code LIMITED_EVIDENCE và giải thích rõ.
        Giải thích bằng tiếng Việt có dấu, tối đa hai câu.
        Trả đúng JSON schema, không thêm văn bản ngoài JSON.
        """;

    public static string Build(PriceSuggestionContext context) =>
        Instructions + "\nPRICING_CONTEXT:\n" + JsonSerializer.Serialize(context);
}
