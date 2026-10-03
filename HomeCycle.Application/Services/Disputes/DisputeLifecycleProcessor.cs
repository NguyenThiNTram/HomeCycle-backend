using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public sealed class DisputeLifecycleProcessor : IOrderLifecycleProcessor
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IDisputeCategoryRepository _categoryRepository;
        private readonly IAgreementFormRepository _agreementRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DisputeLifecycleProcessor> _logger;

        public string Name => "DisputeLifecycle";

        public DisputeLifecycleProcessor(
            IAppointmentRepository appointmentRepository,
            IDisputeRepository disputeRepository,
            IDisputeCategoryRepository categoryRepository,
            IAgreementFormRepository agreementRepository,
            IOrderRepository orderRepository,
            IShipmentRepository shipmentRepository,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<DisputeLifecycleProcessor> logger)
        {
            _appointmentRepository = appointmentRepository;
            _disputeRepository = disputeRepository;
            _categoryRepository = categoryRepository;
            _agreementRepository = agreementRepository;
            _orderRepository = orderRepository;
            _shipmentRepository = shipmentRepository;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken ct = default)
        {
            var processed = 0;
            var policy = await _platformPolicyProvider.GetDisputeConfigAsync(ct);

            var systemNoShowMaintenanceIds =
                await _disputeRepository.GetSystemNoShowMaintenanceCandidateIdsAsync(
                    batchSize,
                    ct);

            foreach (var disputeId in systemNoShowMaintenanceIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await MaintainSystemNoShowAsync(disputeId, ct))
                        processed++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "System no-show recovery failed for Dispute {DisputeId}.",
                        disputeId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            var appointmentIds =
                await _appointmentRepository.GetNoShowCandidateIdsAsync(
                    DateTime.UtcNow,
                    batchSize,
                    ct);

            foreach (var appointmentId in appointmentIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await CreateSystemNoShowAsync(
                        appointmentId,
                        policy.ResponseWindowHours,
                        ct))
                    {
                        processed++;
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "System no-show creation failed for Appointment {AppointmentId}.",
                        appointmentId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            var timeoutIds =
                await _disputeRepository.GetAwaitingResponseTimeoutCandidateIdsAsync(
                    DateTime.UtcNow,
                    batchSize,
                    ct);

            foreach (var disputeId in timeoutIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await EscalateExpiredAsync(disputeId, ct))
                        processed++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Dispute response timeout escalation failed for {DisputeId}.",
                        disputeId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            return processed;
        }


        private async Task<bool> EscalateExpiredAsync(
            Guid disputeId,
            CancellationToken ct)
        {
            IReadOnlyList<notification> moderatorNotifications =
                Array.Empty<notification>();

            Guid? orderIdForRealtime = null;
            DateTime? realtimeAt = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var lockedDispute =
                    await _disputeRepository.GetByIdForUpdateAsync(
                        disputeId,
                        ct);

                if (lockedDispute == null ||
                    lockedDispute.DisputeStatus != (int)DisputeStatus.AwaitingResponse ||
                    !lockedDispute.ResponseDeadlineAt.HasValue ||
                    lockedDispute.ResponseDeadlineAt.Value > DateTime.UtcNow)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var now = DateTime.UtcNow;

                lockedDispute.DisputeStatus = (int)DisputeStatus.Pending;
                lockedDispute.EscalatedAt ??= now;
                lockedDispute.UpdatedAt = now;

                await _disputeRepository.UpdateAsync(lockedDispute, ct);

                moderatorNotifications =
                    await _notificationService.AddPendingForActiveModeratorsAsync(
                        "Tranh chấp quá hạn phản hồi",
                        "Tranh chấp đã hết thời hạn phản hồi và được chuyển sang hàng chờ Moderator.",
                        NotificationTargetType.Dispute,
                        lockedDispute.DisputeId,
                        ct);

                await _auditService.EnqueueAsync(
                    new AuditEvent
                    {
                        Category = AuditCategory.BusinessOperation,
                        Action = AuditActions.DisputeEscalate,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.System,
                        Source = AuditSource.BackgroundJob,
                        TargetType = AuditTargetTypes.Dispute,
                        TargetId = lockedDispute.DisputeId,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["responseDeadlineAt"] = lockedDispute.ResponseDeadlineAt,
                            ["escalatedAt"] = now
                        }
                    },
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                orderIdForRealtime = lockedDispute.OrderId;
                realtimeAt = now;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }

            foreach (var notification in moderatorNotifications)
                await _notificationService.PublishCreatedSafelyAsync(notification);

            if (orderIdForRealtime.HasValue)
            {
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    orderIdForRealtime.Value,
                    realtimeAt!.Value);
            }

            return true;
        }

        private async Task<bool> MaintainSystemNoShowAsync(Guid disputeId, CancellationToken ct)
        {
            var notifications = new List<notification>();
            Guid? orderIdForRealtime = null;
            DateTime? realtimeAt = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var lockedDispute = await _disputeRepository.GetByIdForUpdateAsync(disputeId, ct);

                if (lockedDispute == null ||
                    lockedDispute.SenderId.HasValue ||
                    lockedDispute.DisputeStatus != (int)DisputeStatus.AwaitingResponse ||
                    !lockedDispute.OrderId.HasValue ||
                    !lockedDispute.AppointmentId.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                if (lockedDispute.Origin is not
                    ((int)DisputeOrigin.InspectionNoShow or (int)DisputeOrigin.CollectionNoShow))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var lockedOrder = await _orderRepository.GetByIdForUpdateAsync(
                    lockedDispute.OrderId.Value, ct);

                var lockedAppointment = await _appointmentRepository.GetByIdForUpdateAsync(
                    lockedDispute.AppointmentId.Value, ct);

                if (lockedOrder == null || lockedAppointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var agreement = await _agreementRepository.GetByIdAsync(
                    lockedOrder.AgreementId, ct);

                if (agreement == null)
                    throw new InvalidOperationException(
                        $"Agreement {lockedOrder.AgreementId} was not found.");

                var now = DateTime.UtcNow;

                if (lockedDispute.Origin == (int)DisputeOrigin.InspectionNoShow)
                {
                    var buyerCheckedIn = lockedAppointment.BuyerCheckAt.HasValue;
                    var sellerCheckedIn = lockedAppointment.SellerCheckAt.HasValue;

                    if (buyerCheckedIn && sellerCheckedIn)
                    {
                        lockedDispute.DisputeStatus = (int)DisputeStatus.Closed;
                        lockedDispute.ResolutionSource = (int)DisputeResolutionSource.SystemAutoClosed;
                        lockedDispute.UpdatedAt = now;

                        await _disputeRepository.UpdateAsync(lockedDispute, ct);

                        var recoveryMessage =
                            "Hệ thống ghi nhận cả hai bên đã check-in. Sự cố NO_SHOW đã được tự động đóng.";

                        notifications.Add(await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                agreement.BuyerId,
                                "Sự cố NO_SHOW đã được đóng",
                                recoveryMessage,
                                NotificationTargetType.Dispute,
                                lockedDispute.DisputeId),
                            ct));

                        notifications.Add(await _notificationService.AddPendingAsync(
                            new CreateNotificationCommand(
                                agreement.SellerId,
                                "Sự cố NO_SHOW đã được đóng",
                                recoveryMessage,
                                NotificationTargetType.Dispute,
                                lockedDispute.DisputeId),
                            ct));

                        await _auditService.EnqueueAsync(
                            new AuditEvent
                            {
                                Category = AuditCategory.BusinessOperation,
                                Action = AuditActions.DisputeAutoClose,
                                Outcome = AuditOutcome.Success,
                                ActorType = AuditActorType.System,
                                Source = AuditSource.BackgroundJob,
                                TargetType = AuditTargetTypes.Dispute,
                                TargetId = lockedDispute.DisputeId
                            },
                            ct);
                    }
                    else if (lockedDispute.TargetUserId == null &&
                             buyerCheckedIn != sellerCheckedIn)
                    {
                        var missingUserId = buyerCheckedIn
                            ? agreement.SellerId
                            : agreement.BuyerId;

                        lockedDispute.TargetUserId = missingUserId;
                        lockedDispute.UpdatedAt = now;

                        await _disputeRepository.UpdateAsync(lockedDispute, ct);
                    }
                    else
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return false;
                    }
                }
                else
                {
                    var collectionRecovered =
                        lockedOrder.SellerHandoverConfirmedAt.HasValue ||
                        lockedOrder.BuyerReceivedConfirmedAt.HasValue ||
                        lockedOrder.OrderStatus == (int)OrderStatus.Completed;

                    if (!collectionRecovered)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return false;
                    }

                    lockedDispute.DisputeStatus = (int)DisputeStatus.Closed;
                    lockedDispute.ResolutionSource = (int)DisputeResolutionSource.SystemAutoClosed;
                    lockedDispute.UpdatedAt = now;

                    await _disputeRepository.UpdateAsync(lockedDispute, ct);

                    var recoveryMessage =
                        "Hệ thống ghi nhận giao dịch đã tiếp tục thành công. Sự cố NO_SHOW đã được tự động đóng.";

                    notifications.Add(await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Sự cố NO_SHOW đã được đóng",
                            recoveryMessage,
                            NotificationTargetType.Dispute,
                            lockedDispute.DisputeId),
                        ct));

                    notifications.Add(await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Sự cố NO_SHOW đã được đóng",
                            recoveryMessage,
                            NotificationTargetType.Dispute,
                            lockedDispute.DisputeId),
                        ct));

                    await _auditService.EnqueueAsync(
                        new AuditEvent
                        {
                            Category = AuditCategory.BusinessOperation,
                            Action = AuditActions.DisputeAutoClose,
                            Outcome = AuditOutcome.Success,
                            ActorType = AuditActorType.System,
                            Source = AuditSource.BackgroundJob,
                            TargetType = AuditTargetTypes.Dispute,
                            TargetId = lockedDispute.DisputeId
                        },
                        ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                orderIdForRealtime = lockedOrder.OrderId;
                realtimeAt = lockedDispute.UpdatedAt;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }

            foreach (var notification in notifications)
                await _notificationService.PublishCreatedSafelyAsync(notification);

            if (orderIdForRealtime.HasValue && realtimeAt.HasValue)
            {
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    orderIdForRealtime.Value, realtimeAt.Value);
            }

            return true;
        }

        private async Task<bool> CreateSystemNoShowAsync(
            Guid appointmentId,
            int responseWindowHours,
            CancellationToken ct)
        {
            var notifications = new List<notification>();
            Guid? orderIdForRealtime = null;
            DateTime? realtimeAt = null;

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var appointmentSnapshot =
                    await _appointmentRepository.GetByIdAsync(appointmentId, ct);

                if (appointmentSnapshot == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var agreement = await _agreementRepository.GetByIdAsync(
                    appointmentSnapshot.AgreementId,
                    ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var orderSnapshot =
                    await _orderRepository.GetByAgreementIdAsync(
                        agreement.AgreementId,
                        ct);

                if (orderSnapshot == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                // Giữ lock order trước appointment để cùng thứ tự với OrderService.
                var lockedOrder = await _orderRepository.GetByIdForUpdateAsync(
                    orderSnapshot.OrderId,
                    ct);

                var lockedAppointment =
                    await _appointmentRepository.GetByIdForUpdateAsync(
                        appointmentId,
                        ct);

                if (lockedOrder == null || lockedAppointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var now = DateTime.UtcNow;

                if (lockedOrder.OrderStatus != (int)OrderStatus.Processing ||
                    !lockedAppointment.LateThresholdAt.HasValue ||
                    lockedAppointment.LateThresholdAt.Value > now ||
                    lockedAppointment.AppointmentStatus is not
                        ((int)AppointmentStatus.Scheduled or (int)AppointmentStatus.InProgress))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                DisputeOrigin origin;
                Guid? targetUserId;

                if (lockedAppointment.AppointmentType == (int)AppointmentType.Inspection)
                {
                    var buyerCheckedIn = lockedAppointment.BuyerCheckAt.HasValue;
                    var sellerCheckedIn = lockedAppointment.SellerCheckAt.HasValue;

                    if (buyerCheckedIn && sellerCheckedIn)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return false;
                    }

                    origin = DisputeOrigin.InspectionNoShow;

                    targetUserId =
                        buyerCheckedIn && !sellerCheckedIn
                            ? agreement.SellerId
                            : !buyerCheckedIn && sellerCheckedIn
                                ? agreement.BuyerId
                                : null;
                }
                else if (lockedAppointment.AppointmentType == (int)AppointmentType.Collection)
                {
                    var shipment = await _shipmentRepository.GetByOrderIdAsync(
                        lockedOrder.OrderId,
                        ct);

                    var isDirectCollection =
                        shipment?.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                        shipment?.DeliveryMethod == DeliveryMethod.SellerDelivers;

                    if (!isDirectCollection ||
                        lockedOrder.SellerHandoverConfirmedAt.HasValue ||
                        lockedOrder.BuyerReceivedConfirmedAt.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return false;
                    }

                    origin = DisputeOrigin.CollectionNoShow;
                    targetUserId = null;
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                if (await _disputeRepository.ExistsForAppointmentOriginAsync(
                    lockedAppointment.AppointmentId,
                    origin,
                    ct))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                if (await _disputeRepository.ExistsActiveAsync(
                    DisputeTargetType.Order,
                    lockedOrder.OrderId,
                    ct))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return false;
                }

                var noShowCategory = await _categoryRepository.GetByCodeAsync(
                    "NO_SHOW",
                    ct);

                if (noShowCategory == null ||
                    !noShowCategory.IsActive ||
                    !noShowCategory.TargetTypes.Contains(DisputeTargetType.Order))
                {
                    throw new InvalidOperationException(
                        "Active NO_SHOW dispute category for Order was not found.");
                }

                var systemDispute = new dispute
                {
                    DisputeId = Guid.NewGuid(),
                    SenderId = null,
                    TargetUserId = targetUserId,
                    ModeratorId = null,

                    OrderId = lockedOrder.OrderId,
                    AppointmentId = lockedAppointment.AppointmentId,

                    DisputeTargetType = (int)DisputeTargetType.Order,
                    DisputeCategory = noShowCategory.DisputeCategoryId,
                    Origin = (int)origin,

                    Description = origin == DisputeOrigin.InspectionNoShow
                        ? "Hệ thống ghi nhận lịch kiểm định đã vượt thời gian chờ nhưng chưa đủ check-in từ hai bên."
                        : "Hệ thống ghi nhận lịch giao nhận đã vượt thời gian chờ nhưng chưa có xác nhận giao nhận.",

                    DisputeStatus = (int)DisputeStatus.AwaitingResponse,
                    ResponseDeadlineAt = now.AddHours(responseWindowHours),

                    EscalatedAt = null,
                    ModeratorClaimedAt = null,
                    ResolutionOutcome = null,
                    ResolutionSource = null,

                    CreatedAt = now,
                    UpdatedAt = now,
                    ResolvedAt = null
                };

                await _disputeRepository.AddAsync(systemDispute, ct);

                var buyerMessage =
                    origin == DisputeOrigin.InspectionNoShow &&
                    lockedAppointment.BuyerCheckAt.HasValue &&
                    !lockedAppointment.SellerCheckAt.HasValue
                        ? "Bạn đã check-in nhưng người bán chưa check-in trong thời gian chờ quy định."
                        : "Hệ thống ghi nhận lịch hẹn đã vượt thời gian chờ và tạo một sự cố NO_SHOW để theo dõi.";

                var sellerMessage =
                    origin == DisputeOrigin.InspectionNoShow &&
                    lockedAppointment.SellerCheckAt.HasValue &&
                    !lockedAppointment.BuyerCheckAt.HasValue
                        ? "Bạn đã check-in nhưng người mua chưa check-in trong thời gian chờ quy định."
                        : "Hệ thống ghi nhận lịch hẹn đã vượt thời gian chờ và tạo một sự cố NO_SHOW để theo dõi.";

                notifications.Add(
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.BuyerId,
                            "Lịch hẹn đã vượt thời gian chờ",
                            buyerMessage,
                            NotificationTargetType.Dispute,
                            systemDispute.DisputeId),
                        ct));

                notifications.Add(
                    await _notificationService.AddPendingAsync(
                        new CreateNotificationCommand(
                            agreement.SellerId,
                            "Lịch hẹn đã vượt thời gian chờ",
                            sellerMessage,
                            NotificationTargetType.Dispute,
                            systemDispute.DisputeId),
                        ct));

                await _auditService.EnqueueAsync(
                    new AuditEvent
                    {
                        Category = AuditCategory.BusinessOperation,
                        Action = AuditActions.DisputeCreate,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.System,
                        Source = AuditSource.BackgroundJob,
                        TargetType = AuditTargetTypes.Dispute,
                        TargetId = systemDispute.DisputeId,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["origin"] = origin.ToString(),
                            ["orderId"] = lockedOrder.OrderId,
                            ["appointmentId"] = lockedAppointment.AppointmentId,
                            ["targetUserId"] = targetUserId,
                            ["responseDeadlineAt"] = systemDispute.ResponseDeadlineAt
                        }
                    },
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                orderIdForRealtime = lockedOrder.OrderId;
                realtimeAt = now;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }

            foreach (var notification in notifications)
                await _notificationService.PublishCreatedSafelyAsync(notification);

            if (orderIdForRealtime.HasValue)
            {
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    orderIdForRealtime.Value,
                    realtimeAt!.Value);
            }

            return true;
        }
    }
}
