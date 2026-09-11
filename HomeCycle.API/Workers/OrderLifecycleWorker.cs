using HomeCycle.Application.Interfaces.Services.Orders;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class OrderLifecycleWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderLifecycleWorker> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly int _batchSize;

        public OrderLifecycleWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<OrderLifecycleWorkerOptions> options,
            ILogger<OrderLifecycleWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _pollInterval =
                TimeSpan.FromSeconds(options.Value.PollSeconds);

            _batchSize = options.Value.BatchSize;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Order lifecycle worker started (poll every {PollSeconds}s, batch {BatchSize}).",
                _pollInterval.TotalSeconds,
                _batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();

                var processors = scope.ServiceProvider
                    .GetServices<IOrderLifecycleProcessor>();

                foreach (var processor in processors)
                {
                    try
                    {
                        var processed =
                            await processor.ProcessDueAsync(
                                _batchSize,
                                stoppingToken);

                        if (processed > 0)
                        {
                            _logger.LogInformation(
                                "Order lifecycle processor {Processor} processed {Count} item(s).",
                                processor.Name,
                                processed);
                        }
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Order lifecycle processor {Processor} failed on this poll.",
                            processor.Name);
                    }
                }

                try
                {
                    await Task.Delay(
                        _pollInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Order lifecycle worker stopped.");
        }
    }
}
