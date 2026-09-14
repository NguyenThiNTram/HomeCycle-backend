using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Domain.Entities;

namespace HomeCycle.Application.Commons.Helpers;

public static class BuyPostMatching
{
    private static MatchState Compare<T>(T? expected, T? actual, Func<T, T, bool> matches) where T : struct =>
        !expected.HasValue ? MatchState.NotSpecified : !actual.HasValue ? MatchState.Unknown :
        matches(expected.Value, actual.Value) ? MatchState.Matched : MatchState.NotMatched;

    public static MatchSummaryResponse Evaluate(post buy, post sell)
    {
        var b = buy.Product!; var s = sell.Product!;
        var summary = new MatchSummaryResponse {
            Category = Compare(b.CategoryId, s.CategoryId, (a, v) => a == v),
            ProductType = Compare(b.ProductTypeId, s.ProductTypeId, (a, v) => a == v),
            Brand = Compare(b.BrandId, s.BrandId, (a, v) => a == v),
            Functionality = Compare(b.FunctionalityStatus, s.FunctionalityStatus, (a, v) => (int)v <= (int)a),
            UsageDuration = Compare(b.UsageDuration, s.UsageDuration, (a, v) => v <= a),
            DamageLevel = Compare(b.DamageLevel, s.DamageLevel, (a, v) => (int)v <= (int)a),
            Price = buy.MinExpectedPrice == null && buy.BasePrice == null ? MatchState.NotSpecified :
                sell.BasePrice == null ? MatchState.Unknown :
                (buy.MinExpectedPrice == null || sell.BasePrice >= buy.MinExpectedPrice) &&
                (buy.BasePrice == null || sell.BasePrice <= buy.BasePrice) ? MatchState.Matched : MatchState.NotMatched,
            City = string.IsNullOrWhiteSpace(buy.City) ? MatchState.NotSpecified : string.IsNullOrWhiteSpace(sell.City) ? MatchState.Unknown :
                string.Equals(buy.City.Trim(), sell.City.Trim(), StringComparison.OrdinalIgnoreCase) ? MatchState.Matched : MatchState.NotMatched
        };
        foreach (var expected in b.Product_Attribute_Values)
        {
            var actual = s.Product_Attribute_Values.FirstOrDefault(v => v.AttributeId == expected.AttributeId);
            summary.Attributes[expected.AttributeId] = actual == null ? MatchState.Unknown :
                expected.OptionId == actual.OptionId && expected.ValueText == actual.ValueText &&
                expected.ValueNumber == actual.ValueNumber && expected.ValueBoolean == actual.ValueBoolean ? MatchState.Matched : MatchState.NotMatched;
        }
        return summary;
    }
}
