namespace HomeCycle.Application.DTOs.Requests.SupplierMatching;

public sealed class SupplierMatchAdvancedFilterRequest
{
    public bool RequireFullQuantity { get; set; }
    public bool StrictBudget { get; set; }
    public bool StrictBrand { get; set; }
    public bool SameCityOnly { get; set; }
    public double? MinimumSellerRating { get; set; }
}
