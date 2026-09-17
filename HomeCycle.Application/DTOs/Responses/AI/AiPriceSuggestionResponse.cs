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
    public int RemainingToday { get; set; }
    public DateTimeOffset ResetsAt { get; set; }
}

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
