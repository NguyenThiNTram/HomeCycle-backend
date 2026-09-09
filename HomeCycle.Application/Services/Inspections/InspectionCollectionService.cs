using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Requests.Inspections;
using HomeCycle.Application.DTOs.Responses.Inspections;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Inspections;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Products;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Inspections;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Inspections
{
    public sealed class InspectionCollectionService : IInspectionCollectionService
    {
        private const decimal AmountEpsilon = 0.01m;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IInspectionFormRepository _inspectionFormRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ICollectionAppointmentRepository _collectionRepo;
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IGhnShipmentRepository _ghnShipmentRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IProductRepository _productRepo;
        private readonly IGhnService _ghnService;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;
        private readonly INotificationService _notificationService;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<ScheduleInspectionCollectionRequest> _validator;
        private readonly IValidator<CalculateGhnFeeRequest> _ghnFeeValidator;
        private readonly ILogger<InspectionCollectionService> _logger;

        public InspectionCollectionService(
            IInspectionFormRepository inspectionFormRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IAppointmentRepository appointmentRepo,
            ICollectionAppointmentRepository collectionRepo,
            IShipmentRepository shipmentRepo,
            IGhnShipmentRepository ghnShipmentRepo,
            IPaymentRepository paymentRepo,
            IProductRepository productRepo,
            IGhnService ghnService,
            IPlatformPolicyProvider platformPolicyProvider,
            INotificationService notificationService,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IUnitOfWork unitOfWork,
            IValidator<ScheduleInspectionCollectionRequest> validator,
            IValidator<CalculateGhnFeeRequest> ghnFeeValidator,
            ILogger<InspectionCollectionService> logger)
        {
            _inspectionFormRepo = inspectionFormRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _appointmentRepo = appointmentRepo;
            _collectionRepo = collectionRepo;
            _shipmentRepo = shipmentRepo;
            _ghnShipmentRepo = ghnShipmentRepo;
            _paymentRepo = paymentRepo;
            _productRepo = productRepo;
            _ghnService = ghnService;
            _platformPolicyProvider = platformPolicyProvider;
            _notificationService = notificationService;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _ghnFeeValidator = ghnFeeValidator;
            _logger = logger;
        }

        public async Task<Result<ScheduleInspectionCollectionResponse>> ScheduleAsync(
            Guid inspectionFormId,
            Guid buyerId,
            ScheduleInspectionCollectionRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return Result<ScheduleInspectionCollectionResponse>.Fail(
                    new Error(
                        "Validation.InvalidRequest",
                        string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var formSnapshot = await _inspectionFormRepo.GetByIdAsync(
                inspectionFormId,
                cancellationToken);

            if (formSnapshot == null)
                return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.NotFound);

            var orderSnapshot = await _orderRepo.GetByIdAsync(
                formSnapshot.OrderId,
                cancellationToken);

            if (orderSnapshot == null)
                return Result<ScheduleInspectionCollectionResponse>.Fail(OrderErrors.NotFound);

            var agreement = await _agreementRepo.GetByIdAsync(
                orderSnapshot.AgreementId,
                cancellationToken);

            if (agreement == null)
                return Result<ScheduleInspectionCollectionResponse>.Fail(AgreementErrors.NotFound);

            if (agreement.BuyerId != buyerId)
                return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.BuyerOnly);

            AgreementDetailsDto? agreementDetails = null;

            if (!string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
            {
                try
                {
                    agreementDetails = JsonSerializer.Deserialize<AgreementDetailsDto>(
                        agreement.AgreementDetailsJsonb,
                        JsonOptions);
                }
                catch (JsonException)
                {
                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        new Error("Data.InvalidFormat", "AgreementDetailsJsonb không hợp lệ."));
                }
            }

            var ghnInfo = request.GhnInfo ?? agreementDetails?.GhnInfo;
            var pickupAddress = ResolveAddress(
                request.PickupAddress,
                agreementDetails?.PickupAddress,
                ghnInfo?.Sender?.Address?.AddressDetail);

            var deliveryAddress = ResolveAddress(
                request.DeliveryAddress,
                agreementDetails?.DeliveryAddress,
                ghnInfo?.Receiver?.Address?.AddressDetail);

            if (pickupAddress == null || deliveryAddress == null)
            {
                return Result<ScheduleInspectionCollectionResponse>.Fail(
                    InspectionErrors.CollectionAddressRequired);
            }

            decimal shippingFee;
            CalculateGhnFeeRequest? feeRequest = null;

            if (request.DeliveryMethod == DeliveryMethod.GhnDelivery)
            {
                if (request.PaymentType != PaymentType.Full_Payment)
                {
                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        InspectionErrors.GhnFullPaymentRequired);
                }

                if (ghnInfo == null)
                {
                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        new Error("Ghn.ShippingInfoRequired", "Thiếu thông tin vận chuyển GHN."));
                }

                var product = await _productRepo.GetDetailByPostIdAsync(
                    agreement.PostId,
                    cancellationToken);

                var feeRequestResult = GhnShippingCalculationHelper.BuildFeeRequest(
                    ghnInfo,
                    product);

                if (!feeRequestResult.IsSuccess)
                {
                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        feeRequestResult.Error!);
                }

                feeRequest = feeRequestResult.Data!;

                var feeValidation = await _ghnFeeValidator.ValidateAsync(
                    feeRequest,
                    cancellationToken);

                if (!feeValidation.IsValid)
                {
                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        new Error(
                            "Ghn.InvalidFeeRequest",
                            string.Join(", ", feeValidation.Errors
                                .Select(x => x.ErrorMessage)
                                .Distinct())));
                }

                try
                {
                    var quote = await _ghnService.GetShippingFeeAsync(
                        feeRequest,
                        cancellationToken);

                    shippingFee = Math.Round(
                        quote.TotalFee,
                        0,
                        MidpointRounding.AwayFromZero);
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
                        "Không thể tính phí GHN cho Order {OrderId}.",
                        orderSnapshot.OrderId);

                    return Result<ScheduleInspectionCollectionResponse>.Fail(
                        new Error(
                            "Ghn.CalculateFeeFailed",
                            "Không thể tính phí vận chuyển GHN ở thời điểm hiện tại."));
                }
            }
            else if (request.DeliveryMethod == DeliveryMethod.SellerDelivers)
            {
                shippingFee = Math.Round(
                    request.EstimatedShippingFee ?? 0,
                    0,
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                shippingFee = 0;
            }

            var appointmentPolicy = await _platformPolicyProvider
                .GetAppointmentConfigAsync(cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            notification? collectionNotification = null;

            try
            {
                var form = await _inspectionFormRepo.GetByIdForUpdateAsync(
                    inspectionFormId,
                    cancellationToken);

                if (form == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.NotFound);
                }

                if (form.Revision != request.ExpectedRevision)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.RevisionMismatch);
                }

                if (form.InspectionStatus != (int)InspectionStatus.Accepted)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.AcceptedRequired);
                }

                if (form.Conclusion == (int)InspectionConclusion.Failed)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.FailedCannotCollect);
                }

                if (form.CollectAction.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.CollectActionAlreadySelected);
                }

                var order = await _orderRepo.GetByIdForUpdateAsync(
                    form.OrderId,
                    cancellationToken);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(OrderErrors.NotFound);
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(OrderErrors.InvalidStatus);
                }

                if (await _shipmentRepo.GetByOrderIdAsync(order.OrderId, cancellationToken) != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(ShipmentErrors.AlreadyExists);
                }

                var now = DateTime.UtcNow;
                var collectionDate = request.CollectionDate.UtcDateTime;
                var appointmentId = Guid.NewGuid();
                var collectionAppointmentId = Guid.NewGuid();
                var shipmentId = Guid.NewGuid();

                var supportsLateThreshold =
                    request.DeliveryMethod is DeliveryMethod.SellerDelivers
                        or DeliveryMethod.BuyerPickUp;

                var appointment = new appointment
                {
                    AppointmentId = appointmentId,
                    AgreementId = agreement.AgreementId,
                    AppointmentType = (int)AppointmentType.Collection,
                    AppointmentStatus = (int)AppointmentStatus.Scheduled,
                    LateThresholdAt = supportsLateThreshold
                        ? collectionDate.AddMinutes(appointmentPolicy.LateThresholdMinutes)
                        : null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var collectionAppointment = new collection_appointment
                {
                    CollectionAppointmentId = collectionAppointmentId,
                    AppointmentId = appointmentId,
                    CollectionDate = collectionDate,
                    PickupAddress = pickupAddress,
                    DeliveryAddress = deliveryAddress,
                    DeliveryMethod = request.DeliveryMethod.ToString(),
                    EstimatedShippingFee = shippingFee,
                    GhnShippingInfoJsonb = request.DeliveryMethod == DeliveryMethod.GhnDelivery
                        ? JsonSerializer.Serialize(ghnInfo, JsonOptions)
                        : null
                };

                var shipment = new shipment
                {
                    ShipmentId = shipmentId,
                    OrderId = order.OrderId,
                    CollectionAppointmentId = collectionAppointmentId,
                    DeliveryMethod = request.DeliveryMethod,
                    ShipmentStatus = ShipmentStatus.ReadyToPick,
                    FromName = ghnInfo?.Sender?.FullName,
                    FromPhone = ghnInfo?.Sender?.Phone,
                    PickupAddress = pickupAddress,
                    ToName = ghnInfo?.Receiver?.FullName,
                    ToPhone = ghnInfo?.Receiver?.Phone,
                    DeliveryAddress = deliveryAddress,
                    SellerReadyAt = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _appointmentRepo.AddAsync(appointment, cancellationToken);
                await _collectionRepo.AddAsync(collectionAppointment, cancellationToken);
                await _shipmentRepo.AddAsync(shipment, cancellationToken);

                if (request.DeliveryMethod == DeliveryMethod.GhnDelivery)
                {
                    var largestItem = ghnInfo!.ServiceTypeId == 5
                        ? ghnInfo.Items
                            .OrderByDescending(x => (long)x.WeightGram * x.Quantity)
                            .First()
                        : null;

                    var ghnShipment = new ghn_shipment
                    {
                        GHNShipmentId = Guid.NewGuid(),
                        ShipmentId = shipmentId,
                        ClientOrderCode = $"HC-{shipmentId:N}",
                        GHNOrderCode = null,
                        ServiceTypeId = ghnInfo.ServiceTypeId,
                        FromDistrictId = ghnInfo.Sender!.Address.DistrictId,
                        FromWardCode = ghnInfo.Sender.Address.WardCode,
                        ToDistrictId = ghnInfo.Receiver!.Address.DistrictId,
                        ToWardCode = ghnInfo.Receiver.Address.WardCode,
                        Weight = feeRequest!.WeightGram,
                        Length = ghnInfo.ServiceTypeId == 2
                            ? feeRequest.LengthCm
                            : largestItem?.LengthCm,
                        Width = ghnInfo.ServiceTypeId == 2
                            ? feeRequest.WidthCm
                            : largestItem?.WidthCm,
                        Height = ghnInfo.ServiceTypeId == 2
                            ? feeRequest.HeightCm
                            : largestItem?.HeightCm,
                        CODAmount = 0,
                        PaymentTypeId = 1,
                        InsuranceValue = 0,
                        RequiredNote = ghnInfo.RequiredNote?.Trim().ToUpperInvariant(),
                        GHNServiceFee = null,
                        GHNCodFee = null,
                        GHNTotalFee = null,
                        ExpectedDeliveryAt = null,
                        CreationStatus = GHNCreationStatus.Pending,
                        LastCreateAttemptAt = null,
                        LastSyncedAt = null,
                        LastErrorCode = null,
                        CreatedAt = now
                    };

                    await _ghnShipmentRepo.AddAsync(ghnShipment, cancellationToken);
                }

                var goodsTotal = order.FinalTotalAmount
                    ?? order.OriginalTotalAmount
                    ?? 0;

                if (goodsTotal <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ScheduleInspectionCollectionResponse>.Fail(InspectionErrors.InvalidOrderPrice);
                }

                var finalTotalAmount = goodsTotal + shippingFee;
                var amountPaid = order.AmountPaid ?? 0;
                var additionalPaymentAmount = Math.Max(
                    Math.Round(finalTotalAmount - amountPaid, 0, MidpointRounding.AwayFromZero),
                    0);

                order.FinalTotalAmount = finalTotalAmount;
                order.AmountRemaining = additionalPaymentAmount;
                order.PaymentStatus = additionalPaymentAmount <= AmountEpsilon
                    ? (int)PaymentStatus.Completed
                    : (int)PaymentStatus.Pending;
                order.UpdatedAt = now;

                Guid? paymentId = null;

                if (request.PaymentType == PaymentType.Full_Payment
                    && additionalPaymentAmount > AmountEpsilon)
                {
                    paymentId = Guid.NewGuid();

                    var payment = new payment
                    {
                        PaymentId = paymentId.Value,
                        AgreementId = agreement.AgreementId,
                        OrderId = order.OrderId,
                        PayerId = buyerId,
                        PaymentType = (int)PaymentType.Full_Payment,
                        PaymentMethod = null,
                        Amount = additionalPaymentAmount,
                        Description = $"Thanh toán phần còn lại đơn {order.OrderCode}",
                        PaymentStatus = (int)PaymentStatus.Pending,
                        CreatedAt = now,
                        PaidAt = null,
                        ExpiredAt = null
                    };

                    await _paymentRepo.AddAsync(payment, cancellationToken);
                }

                form.CollectAction = (int)InspectionCollectAction.ScheduleCollection;
                form.Revision++;
                form.UpdatedAt = now;

                await _inspectionFormRepo.UpdateAsync(form, cancellationToken);
                await _orderRepo.UpdateAsync(order, cancellationToken);

                collectionNotification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        agreement.SellerId,
                        "Có lịch thu gom mới",
                        "Người mua đã tạo lịch thu gom sau kiểm định. Vui lòng kiểm tra và chuẩn bị hàng.",
                        NotificationTargetType.Appointment,
                        appointmentId),
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _notificationService.PublishCreatedSafelyAsync(collectionNotification);

                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    order.OrderId,
                    now);

                return Result<ScheduleInspectionCollectionResponse>.Success(
                    new ScheduleInspectionCollectionResponse
                    {
                        InspectionFormId = form.InspectionFormId,
                        Revision = form.Revision,
                        OrderId = order.OrderId,
                        AppointmentId = appointmentId,
                        CollectionAppointmentId = collectionAppointmentId,
                        ShipmentId = shipmentId,
                        PaymentId = paymentId,
                        DeliveryMethod = request.DeliveryMethod,
                        PaymentType = request.PaymentType,
                        EstimatedShippingFee = shippingFee,
                        AdditionalPaymentAmount = additionalPaymentAmount,
                        PaymentRequired = paymentId.HasValue,
                        CollectionDate = collectionDate
                    });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        private static string? ResolveAddress(params string?[] values)
        {
            return values
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?.Trim();
        }
    }
}
