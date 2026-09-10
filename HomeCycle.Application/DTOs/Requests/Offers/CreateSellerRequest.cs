namespace HomeCycle.Application.DTOs.Requests.Offers;
public sealed class CreateSellerRequest
{
    public Guid SellPostId { get; set; }
    public decimal OfferPrice { get; set; }
    public int OfferQuantity { get; set; }
}
