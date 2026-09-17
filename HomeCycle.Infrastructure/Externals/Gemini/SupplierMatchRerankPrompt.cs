using System.Text.Json;
using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Infrastructure.Externals.Gemini;

internal static class SupplierMatchRerankPrompt
{
    public static string Build(
        SupplierDemandContext demand,
        IReadOnlyList<SupplierMatchAiCandidate> candidates)
    {
        var context = new
        {
            demand = new
            {
                categorySpecified = demand.CategoryId.HasValue,
                productTypeSpecified = demand.ProductTypeId.HasValue,
                brandSpecified = demand.BrandId.HasValue,
                modelSpecified = !string.IsNullOrWhiteSpace(demand.NormalizedModelNumber),
                demand.PriceFrom,
                demand.PriceTo,
                demand.Quantity,
                functionalityStatus = demand.FunctionalityStatus?.ToString(),
                damageLevel = demand.DamageLevel?.ToString(),
                demand.UsageDuration,
                citySpecified = !string.IsNullOrWhiteSpace(demand.City),
                requestedAttributeCount = demand.Attributes.Count
            },
            candidates
        };

        return """
Bạn xếp hạng lại các nguồn cung đã được backend HomeCycle lọc và chấm điểm.
Chỉ dùng alias C1, C2... đã cung cấp. Không thêm ứng viên, URL, ID hoặc dữ liệu mới.
Ưu tiên khả năng đáp ứng đúng loại sản phẩm, model, thuộc tính, ngân sách, số lượng và tình trạng.
UNKNOWN là thiếu dữ liệu, không được xem là xung đột. Không loại ứng viên chỉ vì UNKNOWN.
Giữ aiScore trong 0 đến 10. Điểm backend là căn cứ chính; chỉ điều chỉnh khi dữ liệu tiêu chí cho thấy cần đổi thứ tự.
Mỗi giải thích bằng tiếng Việt có dấu, tối đa một câu ngắn. Chỉ dùng reasonCodes trong schema.
Trả đúng JSON schema, không thêm văn bản bên ngoài.

MATCHING_CONTEXT:
""" + JsonSerializer.Serialize(context);
    }
}
