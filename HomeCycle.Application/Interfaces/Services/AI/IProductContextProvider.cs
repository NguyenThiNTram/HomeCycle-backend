using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IProductContextProvider
{
    Task<DynamicProductContext?> BuildDraftAsync(
        AiPriceDraftRequest request,
        CancellationToken cancellationToken = default);
}
