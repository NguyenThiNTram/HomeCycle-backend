using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;
using System.Text.Json.Serialization;

namespace HomeCycle.Application.DTOs.Requests.Posts;

public sealed class CreateBuyPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ProductTypeId { get; set; }
    public FunctionalityStatus? FunctionalityStatus { get; set; }
    public int? UsageDuration { get; set; }
    public DamageLevel? DamageLevel { get; set; }
    public List<ProductAttributeValueRequest>? AttributeValues { get; set; }
    public string? StreetAddress { get; set; }
    public string? Ward { get; set; }
    public string? City { get; set; }
    public PriorityLevel? PriorityLevel { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }
    public int? Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
