using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IExternalUsedPriceSearchService
{
    Task<ExternalUsedPriceSearchResult> SearchAsync(
        DynamicProductContext product,
        CancellationToken cancellationToken = default);
}
