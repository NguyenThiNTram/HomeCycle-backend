using AutoMapper;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.DTOs.Responses.Shipments;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Notifications;
using HomeCycle.Application.Interfaces.Services.Shipments;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Shipments
{
    public class ShipmentService : IShipmentService
    {
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ShipmentService(
            IShipmentRepository shipmentRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _shipmentRepo = shipmentRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<ShipmentSellerReadyResponseDto>> ConfirmSellerReadyAsync(
            Guid shipmentId,
            Guid sellerId,
            CancellationToken ct = default)
        {
            var shipmentSnapshot = await _shipmentRepo.GetByIdAsync(shipmentId, ct);

            if (shipmentSnapshot == null)
                return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.NotFound);

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order = await _orderRepo.GetByIdForUpdateAsync(shipmentSnapshot.OrderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(OrderErrors.NotFound);
                }

                var shipment = await _shipmentRepo.GetByIdForUpdateAsync(shipmentId, ct);

                if (shipment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.NotFound);
                }

                if (shipment.OrderId != order.OrderId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.OrderMismatch);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(AgreementErrors.NotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.Forbidden);
                }

                if (shipment.SellerReadyAt.HasValue)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<ShipmentSellerReadyResponseDto>.Success(
                        _mapper.Map<ShipmentSellerReadyResponseDto>(shipment));
                }

                if (order.OrderStatus != (int)OrderStatus.Processing ||
                    shipment.ShipmentStatus != ShipmentStatus.ReadyToPick)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.SellerReadyNotAllowed);
                }

                if (shipment.DeliveryMethod != DeliveryMethod.GhnDelivery &&
                    shipment.DeliveryMethod != DeliveryMethod.SellerDelivers &&
                    shipment.DeliveryMethod != DeliveryMethod.BuyerPickUp)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<ShipmentSellerReadyResponseDto>.Fail(ShipmentErrors.UnsupportedDeliveryMethod);
                }

                var now = DateTime.UtcNow;

                shipment.SellerReadyAt = now;
                shipment.UpdatedAt = now;

                await _shipmentRepo.UpdateAsync(shipment, ct);

                var notificationMessage = shipment.DeliveryMethod switch
                {
                    DeliveryMethod.GhnDelivery =>
                        "Người bán đã chuẩn bị xong hàng. Đơn hàng đang chờ GHN đến lấy.",

                    DeliveryMethod.BuyerPickUp =>
                        "Người bán đã chuẩn bị xong hàng. Bạn có thể đến nhận hàng theo lịch đã thống nhất.",

                    DeliveryMethod.SellerDelivers =>
                        "Người bán đã chuẩn bị xong hàng và sẽ giao hàng theo lịch đã thống nhất.",

                    _ => throw new InvalidOperationException("Phương thức giao nhận không hợp lệ.")
                };

                var notification = await _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        agreement.BuyerId,
                        "Người bán đã chuẩn bị xong hàng",
                        notificationMessage,
                        NotificationTargetType.Order,
                        order.OrderId),
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                await _notificationService.PublishCreatedSafelyAsync(notification);

                return Result<ShipmentSellerReadyResponseDto>.Success(
                    _mapper.Map<ShipmentSellerReadyResponseDto>(shipment));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }
    }
}
