using HomeCycle.Application.Pricing.Models;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IPriceSuggestionAiClient
{
    Task<AiPriceDecision?> SuggestAsync(
        PriceSuggestionContext context,
        CancellationToken cancellationToken = default);
}
