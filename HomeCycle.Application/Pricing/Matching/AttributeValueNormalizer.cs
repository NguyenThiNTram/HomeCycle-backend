using System.Globalization;
using System.Text.RegularExpressions;
using HomeCycle.Application.Pricing.Models;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.Pricing.Matching;

public static partial class AttributeValueNormalizer
{
    public static string? Normalize(DynamicAttributeValue value)
    {
        if (value.OptionId.HasValue)
            return $"option:{value.OptionId.Value:D}";

        return value.DataType switch
        {
            DataType.Number when value.NumberValue.HasValue =>
                $"number:{value.NumberValue.Value.ToString("G29", CultureInfo.InvariantCulture)}",
            DataType.Boolean when value.BooleanValue.HasValue =>
                value.BooleanValue.Value ? "boolean:true" : "boolean:false",
            DataType.Text when !string.IsNullOrWhiteSpace(value.TextValue) =>
                $"text:{NormalizeText(value.TextValue)}",
            _ => null
        };
    }

    private static string NormalizeText(string value) =>
        Whitespace().Replace(value.Trim().ToLowerInvariant(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
