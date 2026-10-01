namespace HomeCycle.Application.DTOs.Responses.AI;

public sealed class AiPriceSuggestionResponse
{
    public string Status { get; set; } = "NO_RELIABLE_DATA";
    public decimal? SuggestedPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Confidence { get; set; } = "NONE";
    public List<string> ReasonCodes { get; set; } = [];
    public string Explanation { get; set; } = string.Empty;
    public AiPriceEvidenceSummary Evidence { get; set; } = new();
    public List<AiPriceSource> Sources { get; set; } = [];
    public AiPriceBreakdown? Breakdown { get; set; }
    public int RemainingToday { get; set; }
    public DateTimeOffset ResetsAt { get; set; }
}

// Cách tính giá để FE hiển thị trong "Xem cách tính". Các dòng điều chỉnh áp dụng nối tiếp.
public sealed class AiPriceBreakdown
{
    // USED_MARKET | BLENDED | NEW_PRICE_DEPRECIATION
    public string Method { get; set; } = string.Empty;
    public string BaseLabel { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public List<AiPriceAdjustment> Adjustments { get; set; } = [];
    public decimal FinalPrice { get; set; }
    public decimal? NewPriceReference { get; set; }
}

public sealed record AiPriceAdjustment(
    string Label,
    decimal? Percent,
    decimal? Amount);

public sealed class AiPriceEvidenceSummary
{
    public int CompletedTradeSamples { get; set; }
    public int InternalListingSamples { get; set; }
    public int ExternalListingSamples { get; set; }
    public int EquivalentCompletedTradeSamples { get; set; }
    public int EquivalentInternalListingSamples { get; set; }
    public int EquivalentExternalListingSamples { get; set; }
    public int NewMarketPriceSamples { get; set; }
    public int UnknownAttributeSamples { get; set; }
    public bool ExternalSearchFromCache { get; set; }
}

public sealed record AiPriceSource(
    string SourceType,
    string SourceName,
    string SourceUrl,
    DateTimeOffset ObservedAt);
