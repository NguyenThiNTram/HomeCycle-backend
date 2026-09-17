using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;

namespace HomeCycle.Application.DTOs.Requests.SupplierMatching;

public sealed class SupplierMatchDraftRequest
{
    public Guid? CategoryId { get; set; }
    public Guid? ProductTypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string? ModelNumber { get; set; }
    public FunctionalityStatus? FunctionalityStatus { get; set; }
    public DamageLevel? DamageLevel { get; set; }
    public int? UsageDuration { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }
    public int Quantity { get; set; } = 1;
    public string? City { get; set; }
    public List<ProductAttributeValueRequest> AttributeValues { get; set; } = [];
    public SupplierMatchAdvancedFilterRequest? AdvancedFilters { get; set; }
}
