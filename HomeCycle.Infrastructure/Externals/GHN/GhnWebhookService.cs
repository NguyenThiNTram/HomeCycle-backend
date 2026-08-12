using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.GHN;
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
        private readonly IGhnService _ghnService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GhnSettings _settings;
        private readonly ILogger<GhnWebhookService> _logger;

        public GhnWebhookService(
            IGhnShipmentRepository ghnShipmentRepository,
            IShipmentRepository shipmentRepository,
            IGhnService ghnService,
            IUnitOfWork unitOfWork,
            IOptions<GhnSettings> settings,
            ILogger<GhnWebhookService> logger)
        {
            _ghnShipmentRepository = ghnShipmentRepository;
            _shipmentRepository = shipmentRepository;
            _ghnService = ghnService;
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
                return Result.Fail(new Error(
                    "GhnWebhook.InvalidShop",
                    "ShopID trong webhook không khớp với ShopID đã cấu hình."));
            }

            var orderCode = request.OrderCode?.Trim();

            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return Result.Fail(new Error(
                    "GhnWebhook.InvalidPayload",
                    "Webhook GHN không chứa OrderCode."));
            }

            var carrierStatus = request.Status?
                .Trim()
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(carrierStatus))
            {
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
                ghnShipment =
                    await _ghnShipmentRepository.GetByClientOrderCodeAsync(
                        clientOrderCode,
                        cancellationToken);
            }

            if (ghnShipment is null)
            {
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
                return Result.Fail(new Error(
                    "GhnWebhook.OrderCodeConflict",
                    "OrderCode webhook không khớp với vận đơn hiện tại."));
            }

            var shipment = await _shipmentRepository.GetByIdAsync(
                ghnShipment.ShipmentId,
                cancellationToken);

            if (shipment is null)
            {
                return Result.Fail(new Error(
                    "GhnWebhook.ShipmentNotFound",
                    "Không tìm thấy Shipment tương ứng."));
            }

            var now = DateTime.UtcNow;
            var eventTime = request.Time?.UtcDateTime ?? now;

            // Dùng trực tiếp status GHN gửi qua webhook.
            var mappedStatus = GhnStatusMapper.Map(carrierStatus);

            ghnShipment.GHNOrderCode ??= orderCode;
            ghnShipment.GHNStatusCode = carrierStatus;
            ghnShipment.CreationStatus = GHNCreationStatus.Success;
            ghnShipment.LastSyncedAt = now;
            ghnShipment.LastErrorCode = null;

            await _ghnShipmentRepository.UpdateAsync(
                ghnShipment,
                cancellationToken);

            if (mappedStatus.HasValue)
            {
                shipment.ShipmentStatus = mappedStatus.Value;
                shipment.UpdatedAt = now;

                if (mappedStatus.Value == ShipmentStatus.Delivered &&
                    shipment.DeliveredAt is null)
                {
                    shipment.DeliveredAt = eventTime;
                }

                await _shipmentRepository.UpdateAsync(
                    shipment,
                    cancellationToken);
            }
            else
            {
                // Vẫn lưu GHNStatusCode raw nhưng giữ nguyên ShipmentStatus local.
                _logger.LogWarning(
                    "Webhook GHN có status chưa hỗ trợ: {Status}, OrderCode: {OrderCode}",
                    carrierStatus,
                    orderCode);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Đã xử lý webhook GHN. Type={Type}, OrderCode={OrderCode}, Status={Status}",
                request.Type,
                orderCode,
                carrierStatus);

            return Result.Success();
        }

        private async Task MarkFailureAsync(
            ghn_shipment ghnShipment,
            string errorCode,
            CancellationToken cancellationToken)
        {
            // Không cập nhật LastSyncedAt vì lần đồng bộ này thất bại.
            ghnShipment.LastErrorCode = errorCode;

            await _ghnShipmentRepository.UpdateAsync(
                ghnShipment,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
