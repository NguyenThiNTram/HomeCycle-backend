using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Infrastructure.Externals.GHN;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class GhnShipmentCreationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GhnShipmentCreationWorker> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _reclaimAfter;
        private readonly int _batchSize;

        public GhnShipmentCreationWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<GhnSettings> settings,
            ILogger<GhnShipmentCreationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _pollInterval = TimeSpan.FromSeconds(settings.Value.CreationWorkerPollSeconds);
            _batchSize = settings.Value.CreationWorkerBatchSize;
            _reclaimAfter = TimeSpan.FromSeconds(settings.Value.CreationWorkerReclaimAfterSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "GHN shipment creation worker started (poll every {Interval}s, batch {Batch}, reclaim Processing after {Reclaim}s).",
                _pollInterval.TotalSeconds, _batchSize, _reclaimAfter.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var creationService = scope.ServiceProvider
                        .GetRequiredService<IGhnShipmentCreationService>();

                    int processed = await creationService.ProcessPendingAsync(
                        _batchSize, _reclaimAfter, stoppingToken);
                    if (processed > 0)
                        _logger.LogInformation("GHN worker processed {Count} shipment(s).", processed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GHN shipment creation worker failed on this poll.");
                }

                try
                {
                    await Task.Delay(_pollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("GHN shipment creation worker stopped.");
        }
    }
}