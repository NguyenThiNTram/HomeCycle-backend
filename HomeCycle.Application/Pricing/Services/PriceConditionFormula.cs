using System.Text;
using HomeCycle.Application.Validations.Posts;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Pricing.Services;

// Công thức giá đồ cũ theo tình trạng:
// - Hệ số hư hại dựa trên bảng thu mua theo hạng A/B/C (hạng B ≈ 87% hạng A, bể kính −15–20%,
//   hỏng màn hình −40–50%). Hệ số hoạt động và hai mức hư nặng là giá trị khởi đầu, cần hiệu chỉnh
//   bằng giao dịch thật trên HomeCycle.
// - Khấu hao theo năm theo Depreciation Guide của Claims Pages; không khấu hao quá 90%.
public static class PriceConditionFormula
{
    public const decimal MinConditionFactor = 0.08m;
    public const decimal MinRemainingValueRatio = 0.10m;
    public const decimal MaxUsedToNewPriceRatio = 0.85m;
    public const decimal SuggestedRangeRatio = 0.10m;
    public const decimal UsedPriceOutlierBand = 0.40m;
    public const decimal NewPriceOutlierBand = 0.25m;
    public const decimal DefaultAnnualDepreciationRate = 0.10m;

    // Giá máy cũ (đã quy về tình trạng tốt) thấp hơn 5% giá mới coi là bất thường (cọc, linh kiện...).
    public const decimal MinUsedToNewPriceRatio = 0.05m;

    // Mẫu có hệ số thấp hơn mức này định giá theo linh kiện/thanh lý, không quy đổi về máy tốt được.
    public const decimal MinComparableSampleFactor = 0.50m;

    // Thứ tự quan trọng: "máy tính bảng" phải được xét trước "máy tính".
    private static readonly (string[] Keywords, decimal Rate)[] DepreciationRates = new (string[] Keywords, decimal Rate)[]
    {
        (new[] { "điện thoại", "dien thoai", "smartphone", "máy tính bảng", "may tinh bang", "tablet", "ipad" }, 0.20m),
        (new[] { "laptop", "máy tính", "may tinh", "pc", "macbook" }, 0.25m),
        (new[] { "máy giặt", "may giat", "máy rửa chén", "may rua chen", "máy rửa bát", "may rua bat" }, 0.125m),
        (new[] { "tivi", "ti vi", "tv", "máy sấy", "may say" }, 0.083m),
        (new[] { "tủ lạnh", "tu lanh", "tủ đông", "tu dong", "tủ mát", "tu mat", "loa", "dàn âm thanh" }, 0.067m),
        (new[] { "máy lạnh", "may lanh", "điều hòa", "điều hoà", "dieu hoa", "lò vi sóng", "lo vi song",
            "máy hút bụi", "may hut bui", "quạt", "quat", "nồi", "noi com", "bàn ủi", "ban ui", "ấm" }, 0.10m)
    };

    public static decimal FunctionalityFactor(FunctionalityStatus status) => status switch
    {
        FunctionalityStatus.FullyFunctional => 1.00m,
        FunctionalityStatus.PartiallyFunctional => 0.65m,
        FunctionalityStatus.NonFunctional => 0.25m,
        _ => 1.00m
    };

    public static decimal DamageFactor(DamageLevel level) => level switch
    {
        DamageLevel.None => 1.00m,
        DamageLevel.Cosmetic_Damage => 0.87m,
        DamageLevel.Minor_Damage => 0.80m,
        DamageLevel.Moderate_Damage => 0.55m,
        DamageLevel.Severe_Damage => 0.30m,
        DamageLevel.Total_Loss => 0.10m,
        _ => 1.00m
    };

    public static string FunctionalityLabel(FunctionalityStatus status) => status switch
    {
        FunctionalityStatus.FullyFunctional => "Hoạt động hoàn hảo",
        FunctionalityStatus.PartiallyFunctional => "Hoạt động một phần",
        FunctionalityStatus.NonFunctional => "Không hoạt động",
        _ => status.ToString()
    };

    public static string DamageLabel(DamageLevel level) => level switch
    {
        DamageLevel.None => "Không hỏng",
        DamageLevel.Cosmetic_Damage => "Thẩm mỹ - Trầy xước nhẹ",
        DamageLevel.Minor_Damage => "Hư nhẹ - Dễ thay thế",
        DamageLevel.Moderate_Damage => "Hư trung bình",
        DamageLevel.Severe_Damage => "Hư nặng",
        DamageLevel.Total_Loss => "Tổn thất toàn bộ",
        _ => level.ToString()
    };

    public static decimal ConditionFactor(FunctionalityStatus status, DamageLevel level) =>
        Math.Max(MinConditionFactor, FunctionalityFactor(status) * DamageFactor(level));

    // Hệ số của một mẫu giá trên HomeCycle; null khi mẫu thiếu tình trạng.
    public static decimal? SampleConditionFactor(int? functionalityStatus, int? damageLevel)
    {
        if (functionalityStatus is null || damageLevel is null ||
            !Enum.IsDefined(typeof(FunctionalityStatus), functionalityStatus.Value) ||
            !Enum.IsDefined(typeof(DamageLevel), damageLevel.Value))
            return null;

        return ConditionFactor(
            (FunctionalityStatus)functionalityStatus.Value,
            (DamageLevel)damageLevel.Value);
    }

    // Quy giá mẫu về giá máy tình trạng tốt. Mẫu hư nặng/thanh lý bị loại (trả null).
    public static decimal? NormalizeToGoodCondition(decimal price, int? functionalityStatus, int? damageLevel)
    {
        if (price <= 0)
            return null;

        var factor = SampleConditionFactor(functionalityStatus, damageLevel);
        if (factor is null)
            return price;

        return factor.Value < MinComparableSampleFactor ? null : price / factor.Value;
    }

    // Giá AI trích từ web phải nằm trong giới hạn giá đăng bán của hệ thống.
    public static bool IsWithinPostingLimits(decimal price) =>
        price >= PostValidationLimits.MinPrice && price <= PostValidationLimits.MaxPrice;

    public static decimal AnnualDepreciationRate(string? productTypeName)
    {
        var name = (productTypeName ?? string.Empty).Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        if (name.Length == 0)
            return DefaultAnnualDepreciationRate;

        foreach (var (keywords, rate) in DepreciationRates)
        {
            if (keywords.Any(keyword => ContainsWord(name, keyword.Normalize(NormalizationForm.FormC))))
                return rate;
        }

        return DefaultAnnualDepreciationRate;
    }

    public static decimal RemainingValueRatio(decimal annualRate, decimal years) =>
        Math.Max(MinRemainingValueRatio, 1m - annualRate * Math.Max(0m, years));

    // Lọc giá ảo của giá máy cũ (đã quy về tình trạng tốt): bỏ mẫu cao hơn giá mới hoặc thấp hơn
    // 5% giá mới, bỏ mẫu lệch quá ±40% so với giá giữa, rồi lọc IQR khi có từ 4 mẫu.
    public static decimal[] FilterUsedPrices(IEnumerable<decimal> prices, decimal? newPrice)
    {
        var values = prices
            .Where(x => x > 0 && (newPrice is not > 0 ||
                                  (x <= newPrice.Value && x >= newPrice.Value * MinUsedToNewPriceRatio)))
            .OrderBy(x => x)
            .ToArray();
        if (values.Length == 0)
            return values;

        var median = Median(values);
        var banded = values
            .Where(x => x >= median * (1m - UsedPriceOutlierBand) && x <= median * (1m + UsedPriceOutlierBand))
            .ToArray();
        if (banded.Length > 0)
            values = banded;

        if (values.Length >= 4)
        {
            var half = values.Length / 2;
            var firstQuartile = Median(values.Take(half).ToArray());
            var thirdQuartile = Median(values.Skip((values.Length + 1) / 2).ToArray());
            var interquartileRange = thirdQuartile - firstQuartile;
            var filtered = values
                .Where(x => x >= firstQuartile - 1.5m * interquartileRange &&
                            x <= thirdQuartile + 1.5m * interquartileRange)
                .ToArray();
            if (filtered.Length > 0)
                values = filtered;
        }

        return values;
    }

    // Các giá bán mới khớp nhau: bỏ nguồn lệch quá 25% so với giá giữa. Trả mảng rỗng nếu
    // không có ít nhất 2 nguồn khớp. Giá đồng thuận là giá giữa của mảng trả về.
    public static decimal[] AgreeingNewPrices(IEnumerable<decimal> prices)
    {
        var values = prices.Where(x => x > 0).OrderBy(x => x).ToArray();
        if (values.Length < 2)
            return [];

        var median = Median(values);
        var agreed = values
            .Where(x => Math.Abs(x - median) <= median * NewPriceOutlierBand)
            .ToArray();
        return agreed.Length >= 2 ? agreed : [];
    }

    public static decimal Median(IReadOnlyList<decimal> sortedValues)
    {
        var midpoint = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[midpoint - 1] + sortedValues[midpoint]) / 2
            : sortedValues[midpoint];
    }

    // "tv" không được khớp trong "tvbox"; từ khóa có dấu cách khớp theo chuỗi con.
    private static bool ContainsWord(string text, string keyword)
    {
        var index = text.IndexOf(keyword, StringComparison.Ordinal);
        while (index >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var afterIndex = index + keyword.Length;
            var after = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
            if (before && after)
                return true;
            index = text.IndexOf(keyword, index + 1, StringComparison.Ordinal);
        }

        return false;
    }
}
