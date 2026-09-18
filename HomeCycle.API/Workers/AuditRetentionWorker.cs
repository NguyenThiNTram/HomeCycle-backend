using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Services.Audits;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class AuditRetentionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AuditLogOptions _options;
        private readonly ILogger<AuditRetentionWorker> _logger;

        public AuditRetentionWorker(IServiceScopeFactory scopeFactory, IOptions<AuditLogOptions> options, ILogger<AuditRetentionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit retention worker started (retention {RetentionDays} days, interval {IntervalHours}h).",
                _options.Retention.Days, _options.Retention.CleanupIntervalHours);

            try
            {
                await RunCleanupAsync(stoppingToken);

                using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.Retention.CleanupIntervalHours));

                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            _logger.LogInformation("Audit retention worker stopped.");
        }

        private async Task RunCleanupAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IAuditRetentionProcessor>();

                var result = await processor.CleanupAsync(cancellationToken);

                if (result.AuditLogsDeleted > 0 || result.FailedOutboxesDeleted > 0)
                {
                    _logger.LogInformation(
                        "Audit retention cleanup deleted {AuditLogsDeleted} audit logs and {FailedOutboxesDeleted} failed outbox records.",
                        result.AuditLogsDeleted,
                        result.FailedOutboxesDeleted);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Audit retention cleanup failed.");
            }
        }
    }
}
