using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace HomeCycle.Application.Services.GHN
{
    public class GhnShipmentCreationService : IGhnShipmentCreationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IGhnShipmentRepository _ghnShipmentRepo;
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly ICollectionAppointmentRepository _collectionRepo;
        private readonly IGhnService _ghnService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GhnShipmentCreationService> _logger;
        private readonly HomeCycle.Application.Interfaces.Repositories.Inspections.IInspectionFormRepository _inspectionRepo;

        public GhnShipmentCreationService(
            IGhnShipmentRepository ghnShipmentRepo,
            IShipmentRepository shipmentRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            ICollectionAppointmentRepository collectionRepo,
            IGhnService ghnService,
            IUnitOfWork unitOfWork,
            ILogger<GhnShipmentCreationService> logger,
            HomeCycle.Application.Interfaces.Repositories.Inspections.IInspectionFormRepository inspectionRepo)
        {
            _ghnShipmentRepo = ghnShipmentRepo;
            _shipmentRepo = shipmentRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _collectionRepo = collectionRepo;
            _ghnService = ghnService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _inspectionRepo = inspectionRepo;
        }

        public async Task<Result> CancelForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var rows = await _ghnShipmentRepo.GetAllByOrderIdAsync(orderId, cancellationToken);
            Result result = Result.Success();
            foreach (var row in rows)
            {
                var current = await CancelShipmentAsync(orderId, row, cancellationToken);
                if (!current.IsSuccess) result = current;
            }
            return result;
        }

        private async Task<Result> CancelShipmentAsync(Guid orderId, ghn_shipment row, CancellationToken cancellationToken)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

            if (order == null || row == null) return Result.Success();
            var inspection = await _inspectionRepo.GetLatestByOrderIdAsync(orderId, cancellationToken);
            if (inspection?.InspectionStatus == (int)InspectionStatus.Rejected) return Result.Success();
            var shipment = await _shipmentRepo.GetByIdAsync(row.ShipmentId, cancellationToken);
            if (shipment == null || shipment.DeliveryMethod != DeliveryMethod.GhnDelivery) return Result.Success();
            var expected = GhnStateVersion.Capture(row);
            try
            {
                if (string.IsNullOrWhiteSpace(row.GHNOrderCode))
                {
                    if (row.LastCreateAttemptAt.HasValue && row.CreationStatus is GHNCreationStatus.Processing or GHNCreationStatus.Uncertain or GHNCreationStatus.Success)
                    {
                        row.LastErrorCode = "CANCEL:AWAITING_CREATE_RESULT";
                        row.LastSyncedAt = DateTime.UtcNow;
                        await _ghnShipmentRepo.TrySaveCarrierStateAsync(row, shipment, expected, cancellationToken);
                        return Result.Fail(new Error("Ghn.CancellationPending", "Chưa xác định mã vận đơn; cần chờ kết quả tạo đơn hoặc webhook GHN."));
                    }
                }
                else
                {
                    var detail = await _ghnService.GetOrderDetailAsync(row.GHNOrderCode, cancellationToken);
                    if (detail.CarrierStatus != "cancel")
                    {
                        var result = await _ghnService.CancelOrdersAsync(new[] { row.GHNOrderCode }, "GHN-CANCEL-OTHER",
                            order.CancellationReason ?? "Hủy đơn HomeCycle", cancellationToken);
                        if (!result.Single().Result)
                        {
                            row.LastErrorCode = "CANCEL:REFUSED";
                            row.LastSyncedAt = DateTime.UtcNow;
                            await _ghnShipmentRepo.TrySaveCarrierStateAsync(row, shipment, expected, cancellationToken);
                            return Result.Fail(new Error("Ghn.CancellationRefused", result.Single().Message));
                        }
                    }
                    row.GHNStatusCode = "cancel";
                }
                row.LastErrorCode = null;
                row.LastSyncedAt = DateTime.UtcNow;
                shipment.ShipmentStatus = ShipmentStatus.Cancelled;
                shipment.UpdatedAt = DateTime.UtcNow;
                if (!await _ghnShipmentRepo.TrySaveCarrierStateAsync(row, shipment, expected, cancellationToken))
                    return Result.Fail(new Error("Ghn.CancellationPending", "Vận đơn vừa thay đổi; cần xác nhận hủy lại."));
                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is IGhnApiError or HttpRequestException or OperationCanceledException)
            {
                row.LastErrorCode = "CANCEL:UNCONFIRMED";
                row.LastSyncedAt = DateTime.UtcNow;
                await _ghnShipmentRepo.TrySaveCarrierStateAsync(row, shipment, expected, cancellationToken);
                _logger.LogWarning(ex, "GHN cancel needs reconciliation for {OrderId}", orderId);
                return Result.Fail(new Error("Ghn.CancellationPending", "Chưa xác nhận được hủy vận đơn GHN; hệ thống cần đối soát lại."));
            }
        }

        public async Task CancelForOrderSafelyAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            try { await CancelForOrderAsync(orderId, cancellationToken); }
            catch (Exception ex) { _logger.LogError(ex, "GHN cancellation deferred to worker for {OrderId}", orderId); }
        }
        public async Task<int> ProcessPendingAsync(int batchSize, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default)
        {
            var cancellations = await _ghnShipmentRepo.GetCancellationCandidatesAsync(batchSize, cancellationToken);
            foreach (var row in cancellations)
            {
                var shipment = await _shipmentRepo.GetByIdAsync(row.ShipmentId, cancellationToken);
                if (shipment != null) await CancelForOrderSafelyAsync(shipment.OrderId, cancellationToken);
            }
            var candidates = await _ghnShipmentRepo.GetCreationCandidatesAsync(batchSize, reclaimProcessingAfter, cancellationToken);

            int processed = 0;
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await ProcessOneAsync(candidate, reclaimProcessingAfter, cancellationToken))
                        processed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "GhnShipmentCreationService: lỗi không mong muốn khi xử lý vận đơn {GHNShipmentId}",
                        candidate.GHNShipmentId);
                }
            }

            return processed;
        }

        private async Task<bool> ProcessOneAsync(ghn_shipment candidate, TimeSpan reclaimProcessingAfter, CancellationToken ct)
        {
            var shipment = await _shipmentRepo.GetByIdAsync(candidate.ShipmentId, ct);
            if (!HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.IsGhnDelivery(shipment?.DeliveryMethod) ||
                !string.IsNullOrWhiteSpace(candidate.GHNOrderCode)) return false;
            var order = shipment is null
                ? null
                : await _orderRepo.GetByIdAsync(shipment.OrderId, ct);
            var agreement = order is null
                ? null
                : await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

            if (order?.OrderStatus != (int)OrderStatus.Processing || agreement?.AgreementStatus != (int)AgreementStatus.Confirmed)
                return false;
            var inspection = await _inspectionRepo.GetLatestByOrderIdAsync(order.OrderId, ct);
            if (inspection?.InspectionStatus == (int)InspectionStatus.Rejected) return false;
            if (agreement.AgreementType == (int)AgreementType.Inspection)
            {
                if (inspection?.InspectionStatus != (int)InspectionStatus.Accepted ||
                    inspection.Conclusion is null or (int)InspectionConclusion.Failed ||
                    inspection.CollectAction != (int)InspectionCollectAction.ScheduleCollection) return false;
            }
            else if (agreement.AgreementType != (int)AgreementType.No_Inspection ||
                order.PaymentStatus != (int)PaymentStatus.Completed || order.AmountRemaining is null or > 0.01m)
                return false;

            AgreementDetailsDto? details = null;
            if (agreement is not null)
            {
                try
                {
                    details = ParseAgreementDetails(agreement.AgreementDetailsJsonb);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "GhnShipmentCreationService: lỗi parse AgreementDetailsJsonb của agreement {AgreementId}",
                        agreement.AgreementId);
                }
            }

            GhnShippingInfo? info = null;

            if (shipment?.CollectionAppointmentId.HasValue == true)
            {
                var collectionAppointment = await _collectionRepo.GetByIdAsync(
                    shipment.CollectionAppointmentId.Value,
                    ct);

                if (!string.IsNullOrWhiteSpace(collectionAppointment?.GhnShippingInfoJsonb))
                {
                    try
                    {
                        info = JsonSerializer.Deserialize<GhnShippingInfo>(
                            collectionAppointment.GhnShippingInfoJsonb,
                            JsonOptions);
                    }
                    catch (JsonException exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "GhnShipmentCreationService: lỗi parse GhnShippingInfoJsonb của CollectionAppointment {CollectionAppointmentId}.",
                            shipment.CollectionAppointmentId);
                    }
                }
            }

            if (agreement.AgreementType == (int)AgreementType.No_Inspection &&
                HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.IsAgreementGhnDelivery(
                    (AgreementType)agreement.AgreementType, details?.DeliveryMethod))
                info ??= details?.GhnInfo;


            if (info is null || info.Sender?.Address is null || info.Receiver?.Address is null)
            {
                _logger.LogWarning(
                    "GhnShipmentCreationService: vận đơn {GHNShipmentId} thiếu GhnInfo để tạo đơn GHN",
                    candidate.GHNShipmentId);
                await MarkFailedAsync(candidate, "PERMANENT:MISSING_GHN_INFO", ct);
                return true;
            }

            try
            {
                var request = BuildCreateOrderRequest(candidate, info);
                var clientOrderCode = candidate.ClientOrderCode ?? $"HC-{candidate.ShipmentId:N}";

                // Atomic claim: chỉ 1 worker được xử lý, chống tạo trùng đơn GHN.
                var now = DateTime.UtcNow;
                bool claimed = await _ghnShipmentRepo.TryClaimCreationAsync(
                    candidate.ShipmentId,
                    clientOrderCode,
                    now,
                    reclaimProcessingAfter,
                    ct);

                if (!claimed)
                    return false; // đơn đã được worker khác nhận hoặc không còn hợp lệ

                // Đọc lại trạng thái sau claim để giữ nguyên LastCreateAttemptAt/CreationStatus=Processing.
                var claimedShipment = await _ghnShipmentRepo.GetByShipmentIdAsync(candidate.ShipmentId, ct)
                    ?? candidate;

                try
                {
                    var response = await _ghnService.CreateOrderAsync(request, ct);
                    await MarkSuccessAsync(claimedShipment, response, now, ct);
                    var latestOrder = await _orderRepo.GetByIdAsync(shipment!.OrderId, ct);
                    if (latestOrder?.OrderStatus == (int)OrderStatus.Cancelled) await CancelForOrderSafelyAsync(shipment.OrderId, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (ex is IGhnApiError ghnError)
                {
                    _logger.LogWarning(
                        "GhnShipmentCreationService: GHN từ chối tạo đơn {ClientOrderCode}: {CodeMessage}",
                        clientOrderCode, ghnError.CodeMessage);
                    if (ghnError.HttpStatusCode >= 500) await MarkUncertainAsync(claimedShipment, $"GHN:{ghnError.CodeMessage}", ct);
                    else await MarkFailedAsync(claimedShipment, $"PERMANENT:GHN:{ghnError.CodeMessage}", ct);
                }
                catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
                {
                    // Không chắc GHN đã tạo đơn hay chưa (timeout/mất kết nối)
                    // -> chuyển sang Uncertain để tránh retry tạo trùng vận đơn.
                    _logger.LogWarning(ex,
                        "GhnShipmentCreationService: GHN không phản hồi rõ ràng cho {ClientOrderCode}",
                        clientOrderCode);
                    await MarkUncertainAsync(claimedShipment, "GHN_UNCERTAIN", ct);
                }

                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "GhnShipmentCreationService: dữ liệu vận đơn {GHNShipmentId} không đủ/không hợp lệ để tạo đơn GHN",
                    candidate.GHNShipmentId);
                await MarkFailedAsync(candidate, "PERMANENT:INVALID_GHN_DATA", ct);
                return true;
            }
        }

        private async Task MarkSuccessAsync(ghn_shipment shipment, GhnCreateOrderResponse response, DateTime now, CancellationToken ct)
        {
            await _ghnShipmentRepo.SaveCreationResultAsync(shipment.ShipmentId, response, ct);
            _logger.LogInformation(
                "GhnShipmentCreationService: đã tạo vận đơn GHN {OrderCode} cho {GHNShipmentId}",
                response.OrderCode, shipment.GHNShipmentId);
        }

        private Task MarkFailedAsync(ghn_shipment shipment, string errorCode, CancellationToken ct) =>
            _ghnShipmentRepo.SaveCreationFailureAsync(shipment, GHNCreationStatus.Failed, errorCode, ct);

        private Task MarkUncertainAsync(ghn_shipment shipment, string errorCode, CancellationToken ct) =>
            _ghnShipmentRepo.SaveCreationFailureAsync(shipment, GHNCreationStatus.Uncertain, errorCode, ct);
        private static GhnCreateOrderRequest BuildCreateOrderRequest(ghn_shipment row, GhnShippingInfo info)
        {
            var parcel = HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.GetConfirmedParcel(info);
            if (info.Quote == null || info.Quote.InputHash != HomeCycle.Application.Commons.Helpers.GhnShippingCalculationHelper.SnapshotHash(info))
                throw new ArgumentException("Thiếu quote hoặc snapshot GHN đã thay đổi.");
            if (row.Weight != parcel.WeightGram || row.Length != parcel.LengthCm || row.Width != parcel.WidthCm ||
                row.Height != parcel.HeightCm || row.ServiceTypeId != info.ServiceTypeId ||
                row.FromDistrictId != info.Sender?.Address?.DistrictId || row.FromWardCode != info.Sender?.Address?.WardCode ||
                row.ToDistrictId != info.Receiver?.Address?.DistrictId || row.ToWardCode != info.Receiver?.Address?.WardCode)
                throw new ArgumentException("Snapshot shipment không khớp preview GHN.");
            var sender = info.Sender!;
            var receiver = info.Receiver!;
            var senderAddress = sender.Address;
            var receiverAddress = receiver.Address;

            if (string.IsNullOrWhiteSpace(sender.FullName) ||
                string.IsNullOrWhiteSpace(sender.Phone) ||
                string.IsNullOrWhiteSpace(senderAddress.AddressDetail) ||
                string.IsNullOrWhiteSpace(receiver.FullName) ||
                string.IsNullOrWhiteSpace(receiver.Phone) ||
                string.IsNullOrWhiteSpace(receiverAddress.AddressDetail))
                throw new ArgumentException("Thiếu thông tin liên hệ người gửi/nhận GHN.", nameof(row));

            int serviceTypeId = row.ServiceTypeId
                ?? info.ServiceTypeId
                ?? throw new ArgumentException("Thiếu ServiceTypeId GHN.", nameof(row));

            if (serviceTypeId is not (2 or 5))
                throw new ArgumentException("ServiceTypeId GHN chỉ nhận 2 hoặc 5.", nameof(row));

            string requiredNote = row.RequiredNote ?? info.RequiredNote
                ?? throw new ArgumentException("Thiếu RequiredNote GHN.", nameof(row));

            int? toDistrictId = row.ToDistrictId ?? receiverAddress.DistrictId;
            string toWardCode = row.ToWardCode ?? receiverAddress.WardCode;
            if (toDistrictId is null or <= 0 || string.IsNullOrWhiteSpace(toWardCode))
                throw new ArgumentException("Thiếu địa chỉ người nhận GHN (ToDistrictId/ToWardCode).", nameof(row));

            IReadOnlyList<GhnCreateOrderItemRequest> items = (info.Items ?? Array.Empty<GhnItemSnapshotDto>()).Select(item => new GhnCreateOrderItemRequest
                {
                    Name = item.Name,
                    Code = item.Code,
                    Quantity = item.Quantity,
                    WeightGram = item.WeightGram,
                    LengthCm = item.LengthCm,
                    WidthCm = item.WidthCm,
                    HeightCm = item.HeightCm
                }).ToList();

            return new GhnCreateOrderRequest
            {
                ClientOrderCode = row.ClientOrderCode ?? $"HC-{row.ShipmentId:N}",
                FromName = sender.FullName.Trim(),
                FromPhone = sender.Phone.Trim(),
                FromAddress = BuildAddressText(senderAddress),
                FromWardName = senderAddress.WardName?.Trim() ?? string.Empty,
                FromDistrictName = senderAddress.DistrictName?.Trim() ?? string.Empty,
                FromProvinceName = senderAddress.ProvinceName?.Trim() ?? string.Empty,
                ToName = receiver.FullName.Trim(),
                ToPhone = receiver.Phone.Trim(),
                ToAddress = BuildAddressText(receiverAddress),
                FromDistrictId = row.FromDistrictId ?? senderAddress.DistrictId,
                FromWardCode = row.FromWardCode ?? senderAddress.WardCode,
                ToWardName = receiverAddress.WardName,
                ToDistrictName = receiverAddress.DistrictName,
                ToProvinceName = receiverAddress.ProvinceName,
                ParcelCount = info.ParcelCount,
                ToDistrictId = toDistrictId.Value,
                ToWardCode = toWardCode.Trim(),
                ServiceTypeId = serviceTypeId,
                PaymentTypeId = row.PaymentTypeId,
                InsuranceValue = row.InsuranceValue,
                RequiredNote = requiredNote.Trim().ToUpperInvariant(),
                Content = string.IsNullOrWhiteSpace(info.Content) ? "Sản phẩm HomeCycle" : info.Content,
                WeightGram = parcel.WeightGram,
                LengthCm = parcel.LengthCm,
                WidthCm = parcel.WidthCm,
                HeightCm = parcel.HeightCm,
                Items = items
            };
        }

        private static string BuildAddressText(GhnAddressSnapshotDto address)
        {
            var parts = new[]
            {
                address.AddressDetail,
                address.WardName,
                address.DistrictName,
                address.ProvinceName
            }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

            return string.Join(", ", parts);
        }

        private static AgreementDetailsDto? ParseAgreementDetails(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<AgreementDetailsDto>(json, JsonOptions);
        }
    }
}
