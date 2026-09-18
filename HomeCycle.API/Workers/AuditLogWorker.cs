using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class AuditLogWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogWorker> _logger;
        private readonly AuditLogOptions _options;

        public AuditLogWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<AuditLogOptions> options,
            ILogger<AuditLogWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Audit worker started (poll {PollSeconds}s, batch {BatchSize}).",
                _options.Worker.PollIntervalSeconds,
                _options.Worker.BatchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                var processed = 0;

                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var processor =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IAuditOutboxProcessor>();

                    processed =
                        await processor.ProcessBatchAsync(
                            stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Audit worker failed on this poll.");
                }

                if (processed > 0)
                    continue;

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            _options.Worker
                                .PollIntervalSeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Audit worker stopped.");
        }
    }
}
