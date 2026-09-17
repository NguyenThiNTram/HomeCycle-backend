using HomeCycle.Application.DTOs.Requests.AI;
using HomeCycle.Application.DTOs.Responses.AI;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IPriceSuggestionService
{
    Task<AiPriceSuggestionResponse?> SuggestAsync(
        Guid userId,
        AiPriceDraftRequest request,
        CancellationToken cancellationToken = default);
}
