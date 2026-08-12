using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
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
        private readonly IGhnService _ghnService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GhnShipmentCreationService> _logger;

        public GhnShipmentCreationService(
            IGhnShipmentRepository ghnShipmentRepo,
            IShipmentRepository shipmentRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IGhnService ghnService,
            IUnitOfWork unitOfWork,
            ILogger<GhnShipmentCreationService> logger)
        {
            _ghnShipmentRepo = ghnShipmentRepo;
            _shipmentRepo = shipmentRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _ghnService = ghnService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessPendingAsync(int batchSize, TimeSpan reclaimProcessingAfter, CancellationToken cancellationToken = default)
        {
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
            var order = shipment is null
                ? null
                : await _orderRepo.GetByIdAsync(shipment.OrderId, ct);
            var agreement = order is null
                ? null
                : await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

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

            var info = details?.GhnInfo;
            if (info is null || info.Sender?.Address is null || info.Receiver?.Address is null)
            {
                _logger.LogWarning(
                    "GhnShipmentCreationService: vận đơn {GHNShipmentId} thiếu GhnInfo để tạo đơn GHN",
                    candidate.GHNShipmentId);
                await MarkFailedAsync(candidate, "MISSING_GHN_INFO", ct);
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
                    await MarkFailedAsync(claimedShipment, $"GHN:{ghnError.CodeMessage}", ct);
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
                await MarkFailedAsync(candidate, "INVALID_GHN_DATA", ct);
                return true;
            }
        }

        private async Task MarkSuccessAsync(ghn_shipment shipment, GhnCreateOrderResponse response, DateTime now, CancellationToken ct)
        {
            shipment.CreationStatus = GHNCreationStatus.Success;
            shipment.GHNOrderCode = response.OrderCode;
            shipment.GHNServiceFee = response.ServiceFee;
            shipment.GHNCodFee = response.CodFee;
            shipment.GHNTotalFee = response.TotalFee;
            shipment.ExpectedDeliveryAt = response.ExpectedDeliveryAt?.UtcDateTime;
            shipment.LastSyncedAt = now;
            shipment.LastErrorCode = null;

            await _ghnShipmentRepo.UpdateAsync(shipment, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "GhnShipmentCreationService: đã tạo vận đơn GHN {OrderCode} cho {GHNShipmentId}",
                response.OrderCode, shipment.GHNShipmentId);
        }

        private async Task MarkFailedAsync(ghn_shipment shipment, string errorCode, CancellationToken ct)
        {
            shipment.CreationStatus = GHNCreationStatus.Failed;
            shipment.LastErrorCode = errorCode;
            shipment.LastSyncedAt = DateTime.UtcNow;

            await _ghnShipmentRepo.UpdateAsync(shipment, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private async Task MarkUncertainAsync(ghn_shipment shipment, string errorCode, CancellationToken ct)
        {
            shipment.CreationStatus = GHNCreationStatus.Uncertain;
            shipment.LastErrorCode = errorCode;
            shipment.LastSyncedAt = DateTime.UtcNow;

            await _ghnShipmentRepo.UpdateAsync(shipment, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static GhnCreateOrderRequest BuildCreateOrderRequest(ghn_shipment row, GhnShippingInfo info)
        {
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

            IReadOnlyList<GhnCreateOrderItemRequest> items = serviceTypeId == 5
                ? info.Items.Select(item => new GhnCreateOrderItemRequest
                {
                    Name = item.Name,
                    Code = item.Code,
                    Quantity = item.Quantity,
                    WeightGram = item.WeightGram,
                    LengthCm = item.LengthCm,
                    WidthCm = item.WidthCm,
                    HeightCm = item.HeightCm
                }).ToList()
                : Array.Empty<GhnCreateOrderItemRequest>();

            return new GhnCreateOrderRequest
            {
                ClientOrderCode = row.ClientOrderCode ?? $"HC-{row.ShipmentId:N}",
                FromName = sender.FullName.Trim(),
                FromPhone = sender.Phone.Trim(),
                FromAddress = BuildAddressText(senderAddress),
                FromWardName = senderAddress.WardName.Trim(),
                FromDistrictName = senderAddress.DistrictName.Trim(),
                FromProvinceName = senderAddress.ProvinceName.Trim(),
                ToName = receiver.FullName.Trim(),
                ToPhone = receiver.Phone.Trim(),
                ToAddress = BuildAddressText(receiverAddress),
                ToDistrictId = toDistrictId.Value,
                ToWardCode = toWardCode.Trim(),
                ServiceTypeId = serviceTypeId,
                InsuranceValue = row.InsuranceValue,
                RequiredNote = requiredNote.Trim().ToUpperInvariant(),
                Content = null,
                WeightGram = serviceTypeId == 2 ? row.Weight : null,
                LengthCm = serviceTypeId == 2 ? row.Length : null,
                WidthCm = serviceTypeId == 2 ? row.Width : null,
                HeightCm = serviceTypeId == 2 ? row.Height : null,
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