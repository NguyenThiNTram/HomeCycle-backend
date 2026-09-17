using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.SupplierMatching.Services;

public static class SupplierMatchFingerprint
{
    public const string ScoringVersion = "supplier-score-v1";

    public static string Create(SupplierDemandContext demand, SupplierMatchTier tier)
    {
        var canonical = new
        {
            demand.ProductTypeId,
            demand.CategoryId,
            demand.BrandId,
            demand.NormalizedModelNumber,
            functionality = demand.FunctionalityStatus?.ToString(),
            damage = demand.DamageLevel?.ToString(),
            demand.UsageDuration,
            demand.PriceFrom,
            demand.PriceTo,
            demand.Quantity,
            normalizedCity = demand.City,
            attributes = demand.Attributes
                .OrderBy(attribute => attribute.AttributeId)
                .Select(attribute => new
                {
                    attribute.AttributeId,
                    attribute.OptionId,
                    attribute.BooleanValue,
                    attribute.TextValue,
                    attribute.NumberValue
                }),
            advancedFilters = demand.AdvancedFilters,
            scoringVersion = ScoringVersion,
            tier = tier.ToString().ToUpperInvariant()
        };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
