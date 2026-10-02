using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Services.GHN;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public sealed class GhnWebhookService : IGhnWebhookService
    {
        private readonly IGhnShipmentRepository _ghnShipmentRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly IOrderTrackingRealtimeService _orderTrackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GhnSettings _settings;
        private readonly ILogger<GhnWebhookService> _logger;

        public GhnWebhookService(
            IGhnShipmentRepository ghnShipmentRepository,
            IShipmentRepository shipmentRepository,
            IOrderTrackingRealtimeService orderTrackingRealtimeService,
            IUnitOfWork unitOfWork,
            IOptions<GhnSettings> settings,
            ILogger<GhnWebhookService> logger)
        {
            _ghnShipmentRepository = ghnShipmentRepository;
            _shipmentRepository = shipmentRepository;
            _orderTrackingRealtimeService = orderTrackingRealtimeService;
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<Result> ProcessAsync(
    GhnWebhookRequest request,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            /*
             * request.ShopId: GHN gửi trong webhook.
             * _settings.ShopId: backend tự đọc từ appsettings/environment.
             */
            if (request.ShopId <= 0 || request.ShopId != _settings.ShopId)
            {
                _logger.LogWarning(
                    "GHN webhook rejected: ShopIdMismatch. OrderCode={OrderCode}, ReceivedShopId={ReceivedShopId}, ExpectedShopId={ExpectedShopId}",
                    SanitizeForLog(request.OrderCode), request.ShopId, _settings.ShopId);
                return Result.Fail(new Error(
                    "GhnWebhook.InvalidShop",
                    "ShopID trong webhook không khớp với ShopID đã cấu hình."));
            }

            var orderCode = request.OrderCode?.Trim();

            if (string.IsNullOrWhiteSpace(orderCode))
            {
                _logger.LogWarning("GHN webhook payload rejected: MissingOrderCode.");
                return Result.Fail(new Error(
                    "GhnWebhook.InvalidPayload",
                    "Webhook GHN không chứa OrderCode."));
            }

            var carrierStatus = request.Status?
                .Trim()
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(carrierStatus))
            {
                _logger.LogWarning("GHN webhook payload rejected: MissingStatus. OrderCode={OrderCode}", SanitizeForLog(orderCode));
                return Result.Fail(new Error(
                    "GhnWebhook.InvalidPayload",
                    "Webhook GHN không chứa Status."));
            }

            var clientOrderCode = request.ClientOrderCode?.Trim();

            var ghnShipment =
                await _ghnShipmentRepository.GetByGhnOrderCodeAsync(
                    orderCode,
                    cancellationToken);

            // Webhook Create có thể đến trước khi worker lưu GHNOrderCode.
            if (ghnShipment is null &&
                !string.IsNullOrWhiteSpace(clientOrderCode))
            {
                _logger.LogInformation("GHN webhook lookup fallback. OrderCode={OrderCode}, Lookup=ClientOrderCode", SanitizeForLog(orderCode));
                ghnShipment =
                    await _ghnShipmentRepository.GetByClientOrderCodeAsync(
                        clientOrderCode,
                        cancellationToken);
            }

            if (ghnShipment is null)
            {
                _logger.LogWarning("GHN webhook lookup failed: CarrierShipmentNotFound. OrderCode={OrderCode}", SanitizeForLog(orderCode));
                return Result.Fail(new Error(
                    "GhnWebhook.ShipmentNotFound",
                    $"Không tìm thấy vận đơn GHN {orderCode}."));
            }

            if (!string.IsNullOrWhiteSpace(ghnShipment.GHNOrderCode) &&
                !string.Equals(
                    ghnShipment.GHNOrderCode,
                    orderCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("GHN webhook rejected: OrderCodeConflict. IncomingOrderCode={IncomingOrderCode}, StoredOrderCode={StoredOrderCode}",
                    SanitizeForLog(orderCode), SanitizeForLog(ghnShipment.GHNOrderCode));
                return Result.Fail(new Error(
                    "GhnWebhook.OrderCodeConflict",
                    "OrderCode webhook không khớp với vận đơn hiện tại."));
            }

            var shipment = await _shipmentRepository.GetByIdAsync(
                ghnShipment.ShipmentId,
                cancellationToken);

            if (shipment is null)
            {
                _logger.LogWarning("GHN webhook lookup failed: ShipmentNotFound. OrderCode={OrderCode}, ShipmentId={ShipmentId}",
                    SanitizeForLog(orderCode), ghnShipment.ShipmentId);
                return Result.Fail(new Error(
                    "GhnWebhook.ShipmentNotFound",
                    "Không tìm thấy Shipment tương ứng."));
            }

            var expected = GhnStateVersion.Capture(ghnShipment);
            if (!GhnStatusMapper.CanApply(ghnShipment.GHNStatusCode, carrierStatus))
            {
                _logger.LogInformation("GHN webhook status ignored by transition rules. OrderCode={OrderCode}, PreviousStatus={PreviousStatus}, IncomingStatus={IncomingStatus}",
                    SanitizeForLog(orderCode), SanitizeForLog(ghnShipment.GHNStatusCode), SanitizeForLog(carrierStatus));
                return Result.Success();
            }

            // Callback là nguồn cập nhật trạng thái. Không gọi ngược Order Detail GHN
            // vì sandbox có thể vẫn trả ready_to_pick và ghi đè luồng demo webhook.
            var previousCarrierStatus = ghnShipment.GHNStatusCode;
            // Chụp các mốc timeline trước khi cập nhật.
            var previousShipmentStatus = shipment.ShipmentStatus;
            var previousPickedUpAt = shipment.PickedUpAt;
            var previousDeliveredAt = shipment.DeliveredAt;

            var now = DateTime.UtcNow;
            var eventTime = request.Time?.UtcDateTime;

            // Dùng trực tiếp status GHN gửi qua webhook.
            var mappedStatus = GhnStatusMapper.Map(carrierStatus);

            _logger.LogInformation(
                "GHN webhook applying state. OrderCode={OrderCode}, ShipmentId={ShipmentId}, PreviousCarrierStatus={PreviousCarrierStatus}, IncomingCarrierStatus={IncomingCarrierStatus}, PreviousShipmentStatus={PreviousShipmentStatus}, MappedShipmentStatus={MappedShipmentStatus}, EventTime={EventTime}",
                SanitizeForLog(orderCode), shipment.ShipmentId, SanitizeForLog(previousCarrierStatus),
                SanitizeForLog(carrierStatus), previousShipmentStatus, mappedStatus, eventTime);
            if (!eventTime.HasValue && carrierStatus is "picked" or "delivered")
                _logger.LogWarning("GHN webhook missing event time; pickup/delivery timestamp cannot be set. OrderCode={OrderCode}, Status={Status}",
                    SanitizeForLog(orderCode), SanitizeForLog(carrierStatus));

            ghnShipment.GHNOrderCode ??= orderCode;
            ghnShipment.GHNStatusCode = carrierStatus;
            ghnShipment.CreationStatus = GHNCreationStatus.Success;
            ghnShipment.LastSyncedAt = now;
            if (ghnShipment.LastErrorCode?.StartsWith("CANCEL:") != true || carrierStatus == "cancel") ghnShipment.LastErrorCode = null;



            if (mappedStatus.HasValue)
            {
                shipment.ShipmentStatus = mappedStatus.Value;
                shipment.UpdatedAt = now;

                if (carrierStatus == "picked" && eventTime.HasValue &&
                    (!shipment.PickedUpAt.HasValue ||
                     eventTime < shipment.PickedUpAt.Value))
                {
                    shipment.PickedUpAt = eventTime;
                }

                if (mappedStatus.Value == ShipmentStatus.Delivered && eventTime.HasValue &&
                    shipment.DeliveredAt is null)
                {
                    shipment.DeliveredAt = eventTime;
                }


            }
            else
            {
                // Vẫn lưu GHNStatusCode raw nhưng giữ nguyên ShipmentStatus local.
                _logger.LogWarning(
                    "Webhook GHN có status chưa hỗ trợ: {Status}, OrderCode: {OrderCode}",
                    SanitizeForLog(carrierStatus),
                    SanitizeForLog(orderCode));
            }

            if (!await _ghnShipmentRepository.TrySaveCarrierStateAsync(ghnShipment, shipment, expected, cancellationToken))
            {
                _logger.LogWarning(
                    "GHN webhook database update failed: ConcurrentUpdate. OrderCode={OrderCode}, Status={Status}",
                    SanitizeForLog(orderCode), SanitizeForLog(carrierStatus));
                return Result.Fail(new Error("GhnWebhook.ConcurrentUpdate", "Trạng thái vừa thay đổi; GHN cần gửi lại callback."));
            }

            _logger.LogInformation(
                "GHN webhook database update succeeded. OrderCode={OrderCode}, Status={Status}",
                SanitizeForLog(orderCode), SanitizeForLog(carrierStatus));

            var trackingChanged =
                previousCarrierStatus != ghnShipment.GHNStatusCode ||
                previousShipmentStatus != shipment.ShipmentStatus ||
                previousPickedUpAt != shipment.PickedUpAt ||
                previousDeliveredAt != shipment.DeliveredAt;

            // Chỉ phát sau khi dữ liệu đã lưu thành công.
            if (trackingChanged)
            {
                _logger.LogInformation("GHN webhook requesting realtime publication. OrderCode={OrderCode}, OrderId={OrderId}",
                    SanitizeForLog(orderCode), shipment.OrderId);
                await _orderTrackingRealtimeService.PublishByOrderIdSafelyAsync(
                    shipment.OrderId,
                    shipment.UpdatedAt);
            }

            _logger.LogInformation(
                "Đã xử lý webhook GHN. Type={Type}, OrderCode={OrderCode}, Status={Status}",
                SanitizeForLog(request.Type),
                SanitizeForLog(orderCode),
                SanitizeForLog(carrierStatus));

            return Result.Success();
        }

        private static string SanitizeForLog(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private async Task MarkFailureAsync(
            ghn_shipment ghnShipment,
            string errorCode,
            CancellationToken cancellationToken)
        {
            // Không cập nhật LastSyncedAt vì lần đồng bộ này thất bại.
            ghnShipment.LastErrorCode = errorCode;



            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
