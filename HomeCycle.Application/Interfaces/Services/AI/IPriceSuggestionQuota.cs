namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IPriceSuggestionQuota
{
    Task<int> GetDailyLimitAsync(Guid userId, CancellationToken cancellationToken = default);
    DateTimeOffset ResetsAt { get; }
    Task<int> RemainingAsync(Guid userId, int dailyLimit, CancellationToken cancellationToken = default);
    Task<int?> ReserveAsync(Guid userId, int dailyLimit, CancellationToken cancellationToken = default);
}
