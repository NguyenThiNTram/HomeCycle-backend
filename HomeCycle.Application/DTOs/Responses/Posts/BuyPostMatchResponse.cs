namespace HomeCycle.Application.DTOs.Responses.Posts;

public enum MatchState { Matched, NotMatched, NotSpecified, Unknown }

public sealed class MatchSummaryResponse
{
    public MatchState Category { get; set; }
    public MatchState ProductType { get; set; }
    public MatchState Brand { get; set; }
    public MatchState Functionality { get; set; }
    public MatchState UsageDuration { get; set; }
    public MatchState DamageLevel { get; set; }
    public MatchState Price { get; set; }
    public MatchState City { get; set; }
    public Dictionary<Guid, MatchState> Attributes { get; set; } = new();
    public int MatchedCriteriaCount => States.Count(s => s == MatchState.Matched);
    public int EvaluatedCriteriaCount => States.Count(s => s is MatchState.Matched or MatchState.NotMatched);
    private IEnumerable<MatchState> States => new[] { Category, ProductType, Brand, Functionality, UsageDuration, DamageLevel, Price, City }.Concat(Attributes.Values);
}

public sealed class BuyPostMatchResponse
{
    public PostResponse SellPost { get; set; } = null!;
    public MatchSummaryResponse MatchSummary { get; set; } = null!;
}
