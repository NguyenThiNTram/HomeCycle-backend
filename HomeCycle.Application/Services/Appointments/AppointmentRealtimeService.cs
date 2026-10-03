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
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentRealtimeService> _logger;

        public AppointmentRealtimeService(
            IAppointmentRepository appointmentRepository,
            IAgreementFormRepository agreementRepository,
            IAppointmentRealtimePublisher realtimePublisher,
            IServiceProvider serviceProvider,
            ILogger<AppointmentRealtimeService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _agreementRepository = agreementRepository;
            _realtimePublisher = realtimePublisher;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // Gọi sau khi transaction đã commit. Tham số updatedAt của nơi gọi được giữ để không đổi
        // interface; UpdatedAt của event là thời điểm đọc snapshot nên luôn tăng theo thứ tự dữ liệu.
        public async Task PublishUpdatedSafelyAsync(Guid appointmentId, DateTime updatedAt)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var snapshotAt = DateTime.UtcNow;
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

                var snapshot = await BuildSnapshotAsync(appointmentId, agreement.BuyerId, timeout.Token);

                await _realtimePublisher.PublishUpdatedAsync(
                    new[] { agreement.BuyerId, agreement.SellerId },
                    new AppointmentUpdatedResponse
                    {
                        AppointmentId = appointment.AppointmentId,
                        AgreementId = appointment.AgreementId,
                        UpdatedAt = snapshotAt,
                        RescheduledFromAppointmentId = appointment.RescheduledFromAppointmentId,
                        Appointment = snapshot
                    },
                    timeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể phát AppointmentUpdated cho Appointment {AppointmentId}.", appointmentId);
            }
        }

        // Dựng snapshot từ chính GET /appointments/{id} để event và REST không lệch logic.
        // AppointmentService phụ thuộc service này nên phải lấy IAppointmentService lúc gọi, không qua constructor.
        // Đọc theo góc nhìn người mua rồi bỏ mọi field phụ thuộc người xem.
        private async Task<AppointmentRealtimeSnapshotDto?> BuildSnapshotAsync(
            Guid appointmentId,
            Guid buyerId,
            CancellationToken cancellationToken)
        {
            try
            {
                var appointmentService =
                    (IAppointmentService?)_serviceProvider.GetService(typeof(IAppointmentService));

                if (appointmentService == null)
                    return null;

                var detailResult = await appointmentService.GetDetailAsync(appointmentId, buyerId, cancellationToken);

                if (!detailResult.IsSuccess || detailResult.Data == null)
                {
                    _logger.LogWarning(
                        "Không dựng được snapshot Appointment {AppointmentId}: {ErrorCode}; chỉ phát tín hiệu tải lại.",
                        appointmentId,
                        detailResult.Error?.Code);
                    return null;
                }

                return ToSnapshot(detailResult.Data);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Không dựng được snapshot Appointment {AppointmentId}; chỉ phát tín hiệu tải lại.",
                    appointmentId);
                return null;
            }
        }

        private static AppointmentRealtimeSnapshotDto ToSnapshot(AppointmentDetailDto detail)
        {
            return new AppointmentRealtimeSnapshotDto
            {
                AppointmentType = detail.AppointmentType?.ToString(),
                AppointmentStatus = detail.AppointmentStatus?.ToString(),
                LateThresholdAt = detail.LateThresholdAt,
                IsOverdue = detail.IsOverdue,
                CreatedAt = detail.CreatedAt,
                CompletedAt = detail.CompletedAt,
                UpdatedAt = detail.UpdatedAt,

                Inspection = detail.Inspection == null
                    ? null
                    : new AppointmentRealtimeInspectionDto
                    {
                        InspectionDate = detail.Inspection.InspectionDate,
                        InspectionAddress = detail.Inspection.InspectionAddress,
                        InspectionForm = detail.Inspection.InspectionForm == null
                            ? null
                            : new AppointmentRealtimeInspectionFormDto
                            {
                                InspectionFormId = detail.Inspection.InspectionForm.InspectionFormId,
                                Revision = detail.Inspection.InspectionForm.Revision,
                                InspectionStatus = detail.Inspection.InspectionForm.InspectionStatus.ToString(),
                                InspectionMode = detail.Inspection.InspectionForm.InspectionMode.ToString(),
                                Conclusion = detail.Inspection.InspectionForm.Conclusion?.ToString()
                            },
                        CheckIn = new AppointmentRealtimeCheckInDto
                        {
                            BuyerCheckAt = detail.Inspection.CheckIn.BuyerCheckAt,
                            SellerCheckAt = detail.Inspection.CheckIn.SellerCheckAt,
                            CheckInOpenAt = detail.Inspection.CheckIn.CheckInOpenAt
                        }
                    },

                Collection = detail.Collection == null
                    ? null
                    : new AppointmentRealtimeCollectionDto
                    {
                        CollectionDate = detail.Collection.CollectionDate,
                        PickupAddress = detail.Collection.PickupAddress,
                        DeliveryAddress = detail.Collection.DeliveryAddress,
                        DeliveryMethod = detail.Collection.DeliveryMethod?.ToString()
                    },

                Cancellation = detail.Cancellation,

                Reschedule = detail.Reschedule == null
                    ? null
                    : new AppointmentRealtimeRescheduleDto
                    {
                        OriginalAppointmentId = detail.Reschedule.OriginalAppointmentId,
                        ProposalAppointmentId = detail.Reschedule.ProposalAppointmentId,
                        RequestedByUserId = detail.Reschedule.RequestedByUserId,
                        RequestedAt = detail.Reschedule.RequestedAt,
                        ProposedAt = detail.Reschedule.ProposedAt
                    },

                Order = detail.Order == null
                    ? null
                    : new AppointmentRealtimeOrderDto
                    {
                        OrderId = detail.Order.OrderId,
                        OrderCode = detail.Order.OrderCode,
                        ProductName = detail.Order.ProductName,
                        OrderStatus = detail.Order.OrderStatus?.ToString()
                    }
            };
        }
    }
}
