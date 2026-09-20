using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Services.Appointments;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Appointments
{
    public sealed class AppointmentRealtimeService : IAppointmentRealtimeService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IAppointmentRealtimePublisher _realtimePublisher;
        private readonly ILogger<AppointmentRealtimeService> _logger;

        public AppointmentRealtimeService(IAppointmentRepository appointmentRepository, IAgreementFormRepository agreementRepository, IAppointmentRealtimePublisher realtimePublisher, ILogger<AppointmentRealtimeService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _agreementRepository = agreementRepository;
            _realtimePublisher = realtimePublisher;
            _logger = logger;
        }

        public async Task PublishUpdatedSafelyAsync(Guid appointmentId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, timeout.Token);

                if (appointment == null)
                {
                    _logger.LogWarning("Không thể phát AppointmentUpdated vì không tìm thấy Appointment {AppointmentId}.", appointmentId);
                    return;
                }

                var agreement = await _agreementRepository.GetByIdAsync(appointment.AgreementId, timeout.Token);

                if (agreement == null)
                {
                    _logger.LogWarning("Không thể phát AppointmentUpdated vì không tìm thấy Agreement {AgreementId}.", appointment.AgreementId);
                    return;
                }

                await _realtimePublisher.PublishUpdatedAsync(
                    new[] { agreement.BuyerId, agreement.SellerId },
                    new AppointmentUpdatedResponse
                    {
                        AppointmentId = appointment.AppointmentId,
                        AgreementId = appointment.AgreementId,
                        UpdatedAt = updatedAt
                    },
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể phát AppointmentUpdated cho Appointment {AppointmentId}.", appointmentId);
            }
        }
    }
}
