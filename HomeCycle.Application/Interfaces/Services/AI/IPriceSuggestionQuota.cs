namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IPriceSuggestionQuota
{
    int DailyLimit { get; }
    DateTimeOffset ResetsAt { get; }
    Task<int> RemainingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int?> ReserveAsync(Guid userId, CancellationToken cancellationToken = default);
}
