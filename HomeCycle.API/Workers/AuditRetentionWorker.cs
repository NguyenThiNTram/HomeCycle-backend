using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Interfaces.Security;
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
                await Task.WhenAll(
                    RunAuditRetentionLoopAsync(stoppingToken),
                    RunOtpRetentionLoopAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            _logger.LogInformation("Audit retention worker stopped.");
        }
        private async Task RunAuditRetentionLoopAsync(CancellationToken cancellationToken)
        {
            await RunCleanupAsync(cancellationToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.Retention.CleanupIntervalHours));
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await RunCleanupAsync(cancellationToken);
        }

        private async Task RunOtpRetentionLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IOtpRepository>();
                    var deleted = await repository.DeleteExpiredOrUsedAsync(DateTime.UtcNow, cancellationToken);
                    if (deleted > 0)
                        _logger.LogInformation("OTP retention cleanup deleted {Count} record(s).", deleted);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "OTP retention cleanup failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
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
