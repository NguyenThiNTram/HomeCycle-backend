using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Infrastructure.Externals.PayOS;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class PayOsPaymentSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PayOsPaymentSyncWorker> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _retryAfter;
        private readonly int _batchSize;

        public PayOsPaymentSyncWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<PayOSSettings> options,
            ILogger<PayOsPaymentSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var settings = options.Value;

            if (settings.SyncWorkerPollSeconds < 5)
            {
                throw new InvalidOperationException(
                    "PayOS SyncWorkerPollSeconds phải >= 5.");
            }

            if (settings.SyncWorkerBatchSize is < 1 or > 100)
            {
                throw new InvalidOperationException(
                    "PayOS SyncWorkerBatchSize phải từ 1 đến 100.");
            }

            if (settings.SyncWorkerRetryAfterSeconds <
                settings.SyncWorkerPollSeconds)
            {
                throw new InvalidOperationException(
                    "PayOS SyncWorkerRetryAfterSeconds phải >= SyncWorkerPollSeconds.");
            }

            _pollInterval =
                TimeSpan.FromSeconds(
                    settings.SyncWorkerPollSeconds);

            _batchSize =
                settings.SyncWorkerBatchSize;

            _retryAfter =
                TimeSpan.FromSeconds(
                    settings.SyncWorkerRetryAfterSeconds);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "PayOS payment sync worker started. Poll={PollSeconds}s, Batch={BatchSize}, Retry={RetrySeconds}s.",
                _pollInterval.TotalSeconds,
                _batchSize,
                _retryAfter.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var paymentService =
                        scope.ServiceProvider
                            .GetRequiredService<IPaymentService>();

                    var processed =
                        await paymentService.ProcessPendingPayOsSyncAsync(
                            _batchSize,
                            _retryAfter,
                            stoppingToken);

                    if (processed > 0)
                    {
                        _logger.LogInformation(
                            "PayOS payment sync worker reconciled {Count} payment(s).",
                            processed);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "PayOS payment sync worker failed on this poll.");
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
                "PayOS payment sync worker stopped.");
        }
    }
}
