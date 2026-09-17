using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.AI;

public sealed class AiPriceDraftRequest
{
    public AiPriceProductDraft? Product { get; set; }
}

// Dedicated pricing input: no asking price, original price, shipping or uploaded media.
public sealed class AiPriceProductDraft
{
    public Guid ProductTypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string? ProductName { get; set; }
    public string? ModelNumber { get; set; }
    public FunctionalityStatus? FunctionalityStatus { get; set; }
    public DamageLevel? DamageLevel { get; set; }
    public int? UsageDuration { get; set; }
    public List<ProductAttributeValueRequest> AttributeValues { get; set; } = [];
}
