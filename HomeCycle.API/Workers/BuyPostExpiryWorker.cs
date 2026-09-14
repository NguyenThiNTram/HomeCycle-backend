using HomeCycle.Application.Interfaces.Services.Posts;

namespace HomeCycle.API.Workers;

public sealed class BuyPostExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<BuyPostExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IPostService>().ExpireBuyPostsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Không thể đóng tin thu mua hết hạn."); }
        }
    }
}
