using HomeCycle.Application.Interfaces.Services.Appointments;

namespace HomeCycle.API.Workers
{
    public class AppointmentExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentExpiryWorker> _logger;

        public AppointmentExpiryWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var service = scope.ServiceProvider.GetRequiredService<IAppointmentLifecycleJobService>();

                    var count = await service.ExpireOverdueAppointmentsAsync(stoppingToken);

                    if (count > 0)
                    {
                        _logger.LogInformation("Expired {Count} overdue appointments.", count);
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
                        "Appointment expiry worker failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
