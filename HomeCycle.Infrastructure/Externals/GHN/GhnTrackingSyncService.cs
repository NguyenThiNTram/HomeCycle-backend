using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.GHN;
using HomeCycle.Application.Services.GHN;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public sealed class GhnTrackingSyncService : IGhnTrackingSyncService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IGhnShipmentRepository _ghnShipmentRepo;

        public GhnTrackingSyncService(
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IShipmentRepository shipmentRepo,
            IGhnShipmentRepository ghnShipmentRepo)
        {
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _shipmentRepo = shipmentRepo;
            _ghnShipmentRepo = ghnShipmentRepo;
        }

        public async Task<Result<ShipmentTrackingResponse>> SyncByOrderIdAsync(Guid orderId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);

            if (order is null)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error("Order.NotFound", "Không tìm thấy đơn hàng."));
            }

            var agreement = await _agreementRepo.GetByIdAsync(
                order.AgreementId,
                cancellationToken);

            if (agreement is null)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận của đơn hàng."));
            }

            if (agreement.BuyerId != currentUserId &&
                agreement.SellerId != currentUserId)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error("Auth.Forbidden", "Bạn không có quyền xem trạng thái vận chuyển của đơn hàng này."));
            }

            var shipment = await _shipmentRepo.GetByOrderIdAsync(orderId, cancellationToken);

            if (shipment is null)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error("Shipment.NotFound", "Đơn hàng chưa phát sinh vận đơn."));
            }

            if (shipment.DeliveryMethod != DeliveryMethod.GhnDelivery)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error(
                        "Shipment.NotGhnDelivery",
                        "Đơn hàng này không sử dụng dịch vụ giao hàng GHN."));
            }

            var ghnShipment = await _ghnShipmentRepo.GetByShipmentIdAsync(shipment.ShipmentId, cancellationToken);

            if (ghnShipment is null)
            {
                return Result<ShipmentTrackingResponse>.Fail(
                    new Error("Shipment.GhnRecordNotFound", "Không tìm thấy thông tin vận đơn GHN."));
            }

            // Worker chưa tạo xong vận đơn: trả trạng thái local,
            // không gọi GHN vì chưa có GHNOrderCode.
            if (string.IsNullOrWhiteSpace(ghnShipment.GHNOrderCode))
            {
                if (ghnShipment.CreationStatus == GHNCreationStatus.Success)
                {
                    return Result<ShipmentTrackingResponse>.Fail(
                        new Error("Shipment.GhnOrderCodeMissing", "Vận đơn được đánh dấu đã tạo nhưng chưa có mã GHN."));
                }

                return Result<ShipmentTrackingResponse>.Success(
                    BuildResponse(
                        orderId,
                        shipment,
                        ghnShipment,
                        isStale: false));
            }

            return Result<ShipmentTrackingResponse>.Success(
                BuildResponse(
                    orderId,
                    shipment,
                    ghnShipment,
                    isStale: false,
                    message: "Trạng thái vận chuyển được cập nhật qua webhook GHN."));
        }

        private static ShipmentTrackingResponse BuildResponse(  Guid orderId, shipment shipment, ghn_shipment ghnShipment, bool isStale, string? message = null)
        {
            return new ShipmentTrackingResponse
            {
                OrderId = orderId,
                ShipmentId = shipment.ShipmentId,
                DeliveryMethod = shipment.DeliveryMethod,
                CreationStatus = ghnShipment.CreationStatus,
                TrackingCode = ghnShipment.GHNOrderCode,
                CarrierStatus = ghnShipment.GHNStatusCode,
                IsTerminal = GhnStatusMapper.IsTerminal(ghnShipment.GHNStatusCode),
                CarrierOperationError = ghnShipment.LastErrorCode,
                ShipmentStatus = shipment.ShipmentStatus,
                ExpectedDeliveryAt = ghnShipment.ExpectedDeliveryAt,
                DeliveredAt = shipment.DeliveredAt,
                LastSyncedAt = ghnShipment.LastSyncedAt,
                IsStale = isStale,
                Message = message ??
                          GetTrackingMessage(
                              ghnShipment.CreationStatus,
                              ghnShipment.GHNStatusCode)
            };
        }

        private static string GetTrackingMessage( GHNCreationStatus creationStatus, string? carrierStatus)
        {
            if (string.IsNullOrWhiteSpace(carrierStatus))
            {
                return creationStatus switch
                {
                    GHNCreationStatus.Pending => "Vận đơn GHN đang chờ được khởi tạo.",

                    GHNCreationStatus.Processing => "Vận đơn GHN đang được khởi tạo.",

                    GHNCreationStatus.Failed => "Không thể khởi tạo vận đơn GHN. Hệ thống sẽ thử lại.",

                    GHNCreationStatus.Uncertain => "Chưa xác định được kết quả tạo vận đơn GHN.",

                    GHNCreationStatus.Success => "Vận đơn GHN đã được tạo.",

                    _ => "Chưa có thông tin vận chuyển."
                };
            }

            return carrierStatus.Trim().ToLowerInvariant() switch
            {
                "ready_to_pick" => "Đã tạo vận đơn, đang chờ GHN lấy hàng.",

                "picking" or "money_collect_picking" => "Nhân viên GHN đang đến lấy hàng.",

                "picked" or "storing" or "transporting" or "sorting" => "GHN đã lấy hàng và đang vận chuyển.",

                "delivering" or "money_collect_delivering" => "Đơn hàng đang được giao đến người nhận.",

                "delivery_fail" => "Lần giao hàng gần nhất chưa thành công.",

                "delivered" => "Đơn hàng đã được giao thành công.",

                "waiting_to_return"
                    or "return"
                    or "return_transporting"
                    or "return_sorting"
                    or "returning" =>
                    "Đơn hàng đang được hoàn về người gửi.",

                "return_fail" => "Lần hoàn hàng gần nhất chưa thành công.",

                "returned" => "Đơn hàng đã được hoàn về người gửi.",

                "cancel" => "Vận đơn đã bị hủy.",

                "damage" => "Hàng hóa được GHN ghi nhận bị hư hỏng.",

                "lost" => "Hàng hóa được GHN ghi nhận bị thất lạc.",

                "scrap" => "Hàng hóa đã được GHN ghi nhận tiêu hủy.",
                "exception" => "Vận đơn đang được GHN xử lý ngoại lệ.",

                _ => "Trạng thái vận chuyển vừa được cập nhật."
            };
        }
    }
}
