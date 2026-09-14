using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Domain.Enums;
using System.Text.Json.Serialization;

namespace HomeCycle.Application.DTOs.Requests.Posts;

/// <summary>Omitted fields are preserved; explicit null clears optional criteria.</summary>
public sealed class UpdateBuyPostRequest
{
    [JsonIgnore]
    public HashSet<string> ChangedProperties { get; } = new();
    private string? _Title;
    public string? Title { get => _Title; set { _Title = value; ChangedProperties.Add(nameof(Title)); } }
    private string? _Description;
    public string? Description { get => _Description; set { _Description = value; ChangedProperties.Add(nameof(Description)); } }
    private Guid? _BrandId;
    public Guid? BrandId { get => _BrandId; set { _BrandId = value; ChangedProperties.Add(nameof(BrandId)); } }
    private Guid? _CategoryId;
    public Guid? CategoryId { get => _CategoryId; set { _CategoryId = value; ChangedProperties.Add(nameof(CategoryId)); } }
    private Guid? _ProductTypeId;
    public Guid? ProductTypeId { get => _ProductTypeId; set { _ProductTypeId = value; ChangedProperties.Add(nameof(ProductTypeId)); } }
    private FunctionalityStatus? _FunctionalityStatus;
    public FunctionalityStatus? FunctionalityStatus { get => _FunctionalityStatus; set { _FunctionalityStatus = value; ChangedProperties.Add(nameof(FunctionalityStatus)); } }
    private int? _UsageDuration;
    public int? UsageDuration { get => _UsageDuration; set { _UsageDuration = value; ChangedProperties.Add(nameof(UsageDuration)); } }
    private DamageLevel? _DamageLevel;
    public DamageLevel? DamageLevel { get => _DamageLevel; set { _DamageLevel = value; ChangedProperties.Add(nameof(DamageLevel)); } }
    private List<ProductAttributeValueRequest>? _AttributeValues;
    public List<ProductAttributeValueRequest>? AttributeValues { get => _AttributeValues; set { _AttributeValues = value; ChangedProperties.Add(nameof(AttributeValues)); } }
    private string? _StreetAddress;
    public string? StreetAddress { get => _StreetAddress; set { _StreetAddress = value; ChangedProperties.Add(nameof(StreetAddress)); } }
    private string? _Ward;
    public string? Ward { get => _Ward; set { _Ward = value; ChangedProperties.Add(nameof(Ward)); } }
    private string? _City;
    public string? City { get => _City; set { _City = value; ChangedProperties.Add(nameof(City)); } }
    private PriorityLevel? _PriorityLevel;
    public PriorityLevel? PriorityLevel { get => _PriorityLevel; set { _PriorityLevel = value; ChangedProperties.Add(nameof(PriorityLevel)); } }
    private decimal? _PriceFrom;
    public decimal? PriceFrom { get => _PriceFrom; set { _PriceFrom = value; ChangedProperties.Add(nameof(PriceFrom)); } }
    private decimal? _PriceTo;
    public decimal? PriceTo { get => _PriceTo; set { _PriceTo = value; ChangedProperties.Add(nameof(PriceTo)); } }
    private int? _Quantity;
    public int? Quantity { get => _Quantity; set { _Quantity = value; ChangedProperties.Add(nameof(Quantity)); } }
    private DateTime? _ExpiryDate;
    public DateTime? ExpiryDate { get => _ExpiryDate; set { _ExpiryDate = value; ChangedProperties.Add(nameof(ExpiryDate)); } }
}
