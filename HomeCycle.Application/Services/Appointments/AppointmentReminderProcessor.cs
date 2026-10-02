using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Appointments
{
    public sealed class AppointmentReminderProcessor : IAppointmentReminderProcessor
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AppointmentReminderProcessor> _logger;

        public AppointmentReminderProcessor(
            IAppointmentRepository appointmentRepository,
            IAgreementFormRepository agreementRepository,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            ILogger<AppointmentReminderProcessor> logger)
        {
            _appointmentRepository = appointmentRepository;
            _agreementRepository = agreementRepository;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var policy = await _platformPolicyProvider.GetAppointmentConfigAsync(
                cancellationToken);

            var candidateIds =
                await _appointmentRepository.GetReminderCandidateIdsAsync(
                    DateTime.UtcNow,
                    policy.ReminderBeforeMinutes,
                    batchSize,
                    cancellationToken);

            var processedCount = 0;

            foreach (var appointmentId in candidateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await ProcessOneAsync(
                        appointmentId,
                        policy.ReminderBeforeMinutes,
                        cancellationToken))
                    {
                        processedCount++;
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Không thể gửi reminder cho Appointment {AppointmentId}.",
                        appointmentId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            return processedCount;
        }

        private async Task<bool> ProcessOneAsync(
            Guid appointmentId,
            int reminderBeforeMinutes,
            CancellationToken cancellationToken)
        {
            var pendingNotifications = new List<notification>();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var appointment =
                    await _appointmentRepository.GetByIdForUpdateAsync(
                        appointmentId,
                        cancellationToken);

                if (appointment == null ||
                    appointment.AppointmentStatus !=
                        (int)AppointmentStatus.Scheduled ||
                    appointment.ReminderSentAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return false;
                }

                var agreement =
                    await _agreementRepository.GetByIdAsync(
                        appointment.AgreementId,
                        cancellationToken);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return false;
                }

                var summaries =
                    await _appointmentRepository
                        .GetAppointmentSummariesByAgreementIdAsync(
                            appointment.AgreementId,
                            cancellationToken);

                var currentSummary =
                    summaries.FirstOrDefault(
                        x => x.AppointmentId ==
                             appointment.AppointmentId);

                if (!currentSummary?.ScheduledAt.HasValue ?? true)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return false;
                }

                var now = DateTime.UtcNow;
                var scheduledAt = currentSummary!.ScheduledAt!.Value;
                var reminderWindowEnd =
                    now.AddMinutes(reminderBeforeMinutes);

                if (scheduledAt <= now ||
                    scheduledAt > reminderWindowEnd)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return false;
                }

                var recipientIds = new HashSet<Guid>();

                if (appointment.AppointmentType ==
                    (int)AppointmentType.Inspection)
                {
                    if (!appointment.BuyerCheckAt.HasValue)
                        recipientIds.Add(agreement.BuyerId);

                    if (!appointment.SellerCheckAt.HasValue)
                        recipientIds.Add(agreement.SellerId);
                }
                else if (appointment.AppointmentType ==
                         (int)AppointmentType.Collection)
                {
                    recipientIds.Add(agreement.BuyerId);
                    recipientIds.Add(agreement.SellerId);
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        cancellationToken);

                    return false;
                }

                var minutesUntilStart = Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (scheduledAt - now).TotalMinutes));

                var appointmentLabel =
                    appointment.AppointmentType ==
                    (int)AppointmentType.Inspection
                        ? "kiểm định"
                        : "giao nhận";

                var reminderMessage =
                    $"Lịch {appointmentLabel} của bạn sẽ bắt đầu trong khoảng " +
                    $"{minutesUntilStart} phút. Vui lòng kiểm tra thời gian và " +
                    "địa điểm trong chi tiết lịch hẹn.";

                foreach (var recipientId in recipientIds)
                {
                    pendingNotifications.Add(
                        await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                recipientId,
                                "Sắp đến lịch hẹn",
                                reminderMessage,
                                NotificationTargetType.Appointment,
                                appointment.AppointmentId),
                            cancellationToken));
                }

                appointment.ReminderSentAt = now;

                await _appointmentRepository.UpdateAsync(
                    appointment,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync(
                    cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    cancellationToken);

                throw;
            }

            foreach (var notification in pendingNotifications)
            {
                await _notificationService.PublishCreatedSafelyAsync(
                    notification);
            }

            return true;
        }
    }
}
