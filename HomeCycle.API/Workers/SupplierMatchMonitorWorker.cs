using HomeCycle.Application.Interfaces.Services.SupplierMatching;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers;

public sealed class SupplierMatchMonitorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SupplierMatchMonitorWorkerOptions> options,
    ILogger<SupplierMatchMonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Supplier match monitor is disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.PollMinutes));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ISupplierMatchMonitorService>()
                    .RunBatchAsync(settings.BatchSize, settings.MinimumScore,
                        settings.ImprovementDelta, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Supplier match monitor batch failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
