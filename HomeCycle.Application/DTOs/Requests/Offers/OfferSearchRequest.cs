using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
namespace HomeCycle.Application.DTOs.Requests.Offers;
public sealed class OfferSearchRequest : PaginationRequest
{
    public Guid? PostId { get; set; }
    public Guid? BuyPostId { get; set; }
    public OfferStatus? Status { get; set; }
}
