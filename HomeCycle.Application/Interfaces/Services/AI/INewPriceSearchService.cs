using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface INewPriceSearchService
{
    Task<NewPriceSearchResult> SearchAsync(
        DynamicProductContext product,
        CancellationToken cancellationToken = default);
}
