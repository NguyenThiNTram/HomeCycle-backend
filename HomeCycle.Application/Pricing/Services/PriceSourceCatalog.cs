using System.Text.RegularExpressions;

namespace HomeCycle.Application.Pricing.Services;

// Mức uy tín của nguồn giá, số nhỏ hơn là uy tín hơn. Khi tính giá, hệ thống lấy nguồn
// uy tín nhất trước và chỉ xuống mức dưới khi chưa đủ mẫu.
public enum PriceSourceTier
{
    HomeCycleTrade = 1,      // Giao dịch đã hoàn tất trên HomeCycle
    HomeCycleListing = 2,    // Tin đang bán trên HomeCycle
    OfficialBrand = 3,       // Trang chính hãng (chỉ dùng cho giá mới)
    MajorRetailer = 4,       // Chuỗi bán lẻ lớn (giá mới và mục máy cũ của họ)
    Classified = 5,          // Trang rao vặt lớn (chỉ dùng cho giá máy cũ)
    Untrusted = 99           // Không nằm trong danh mục: bị loại
}

public static partial class PriceSourceCatalog
{
    private static readonly string[] OfficialBrandDomains =
    {
        "apple.com", "samsung.com", "mi.com", "xiaomi.com", "oppo.com", "vivo.com", "realme.com",
        "lg.com", "sony.com", "sony.com.vn", "panasonic.com", "sharp.vn", "electrolux.vn",
        "daikin.com.vn", "toshiba-lifestyle.com", "philips.com.vn", "aqua-vietnam.vn",
        "asus.com", "dell.com", "lenovo.com", "hp.com", "acer.com", "msi.com"
    };

    private static readonly string[] MajorRetailerDomains =
    {
        "thegioididong.com", "dienmayxanh.com", "topzone.vn", "fptshop.com.vn", "cellphones.com.vn",
        "hoanghamobile.com", "nguyenkim.com", "dienmaycholon.com", "mediamart.vn", "didongviet.vn",
        "viettelstore.vn", "shopdunk.com", "phongvu.vn", "gearvn.com", "hacom.vn", "anphatpc.com.vn",
        "dienmaythienhoa.vn", "pico.vn"
    };

    private static readonly string[] ClassifiedDomains =
    {
        "chotot.com", "nhattao.com", "muaban.net"
    };

    // Các tên miền thuộc cùng một công ty được gộp thành một nhà bán lẻ (giá giống nhau).
    private static readonly Dictionary<string, string> RetailerGroups = new(StringComparer.Ordinal)
    {
        ["dienmayxanh.com"] = "thegioididong.com",
        ["topzone.vn"] = "thegioididong.com",
        ["xiaomi.com"] = "mi.com",
        ["sony.com.vn"] = "sony.com"
    };

    // Tên miền dùng để gợi ý Gemini tìm đúng chỗ.
    public static IReadOnlyList<string> NewPriceSearchDomains { get; } =
        OfficialBrandDomains.Take(7).Concat(MajorRetailerDomains.Take(8)).ToArray();

    public static IReadOnlyList<string> UsedPriceSearchDomains { get; } =
        MajorRetailerDomains.Take(8).Concat(ClassifiedDomains).ToArray();

    public static PriceSourceTier Classify(string? domain)
    {
        var host = NormalizeHost(domain);
        if (host is null)
            return PriceSourceTier.Untrusted;
        if (MatchEntry(host, OfficialBrandDomains) is not null)
            return PriceSourceTier.OfficialBrand;
        if (MatchEntry(host, MajorRetailerDomains) is not null)
            return PriceSourceTier.MajorRetailer;
        if (MatchEntry(host, ClassifiedDomains) is not null)
            return PriceSourceTier.Classified;
        return PriceSourceTier.Untrusted;
    }

    // Khóa nhà bán lẻ dùng để đếm nguồn độc lập: m.shop.vn, www.shop.vn và shop.vn là một;
    // các tên miền cùng công ty cũng là một. Null nếu không thuộc danh mục.
    public static string? RetailerKey(string? domain)
    {
        var host = NormalizeHost(domain);
        if (host is null)
            return null;
        var entry = MatchEntry(host, OfficialBrandDomains)
                    ?? MatchEntry(host, MajorRetailerDomains)
                    ?? MatchEntry(host, ClassifiedDomains);
        if (entry is null)
            return null;
        return RetailerGroups.TryGetValue(entry, out var group) ? group : entry;
    }

    public static bool AcceptsNewPrice(PriceSourceTier tier) =>
        tier is PriceSourceTier.OfficialBrand or PriceSourceTier.MajorRetailer;

    public static bool AcceptsUsedPrice(PriceSourceTier tier) =>
        tier is PriceSourceTier.MajorRetailer or PriceSourceTier.Classified;

    // Gemini API không trả field Domain và Uri là link chuyển hướng của Google,
    // nên lấy tên miền theo thứ tự: Domain → Title (thường là tên miền) → host của Uri.
    public static string? ResolveDomain(string? domain, string? title, string? uri)
    {
        var fromDomain = NormalizeHost(domain);
        if (fromDomain is not null)
            return fromDomain;

        var fromTitle = NormalizeHost(title);
        if (fromTitle is not null)
            return fromTitle;

        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
            !parsed.Host.EndsWith("vertexaisearch.cloud.google.com", StringComparison.OrdinalIgnoreCase))
            return NormalizeHost(parsed.Host);

        return null;
    }

    public static string TrustLabel(PriceSourceTier tier) => tier switch
    {
        PriceSourceTier.HomeCycleTrade => "Giao dịch HomeCycle",
        PriceSourceTier.HomeCycleListing => "Tin đăng HomeCycle",
        PriceSourceTier.OfficialBrand => "Trang chính hãng",
        PriceSourceTier.MajorRetailer => "Nhà bán lẻ lớn",
        PriceSourceTier.Classified => "Trang rao vặt",
        _ => "Không xác định"
    };

    private static string? NormalizeHost(string? value)
    {
        var host = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(host))
            return null;
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];
        return HostPattern().IsMatch(host) ? host : null;
    }

    // Chọn mục khớp dài nhất để "sony.com.vn" không bị nhận nhầm thành "sony.com".
    private static string? MatchEntry(string host, IEnumerable<string> domains) =>
        domains
            .Where(d => host == d || host.EndsWith("." + d, StringComparison.Ordinal))
            .OrderByDescending(d => d.Length)
            .FirstOrDefault();

    [GeneratedRegex(@"^[a-z0-9-]+(\.[a-z0-9-]+)+$")]
    private static partial Regex HostPattern();
}
