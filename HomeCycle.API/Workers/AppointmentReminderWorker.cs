using HomeCycle.Application.Interfaces.Services.Appointments;
using Microsoft.Extensions.Options;

namespace HomeCycle.API.Workers
{
    public sealed class AppointmentReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderWorker> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly int _batchSize;

        public AppointmentReminderWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<AppointmentReminderWorkerOptions> options,
            ILogger<AppointmentReminderWorker> logger)
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
                "Appointment reminder worker started " +
                "(poll every {PollSeconds}s, batch {BatchSize}).",
                _pollInterval.TotalSeconds,
                _batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var processor =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IAppointmentReminderProcessor>();

                    var processed =
                        await processor.ProcessDueAsync(
                            _batchSize,
                            stoppingToken);

                    if (processed > 0)
                    {
                        _logger.LogInformation(
                            "Appointment reminder worker processed " +
                            "{Count} appointment(s).",
                            processed);
                    }
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
                        "Appointment reminder worker failed " +
                        "on this poll.");
                }

                try
                {
                    await Task.Delay(
                        _pollInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Appointment reminder worker stopped.");
        }
    }
}
