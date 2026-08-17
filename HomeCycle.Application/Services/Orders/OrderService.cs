using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IShipmentRepository _shipmentRepo;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IOrderRepository orderRepo, IAgreementFormRepository agreementRepo, IAppointmentRepository appointmentRepo,
            IShipmentRepository shipmentRepo, IUnitOfWork unitOfWork)
        {
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _appointmentRepo = appointmentRepo;
            _shipmentRepo = shipmentRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<OrderListItemDto>>> GetMyOrdersAsync(
            Guid userId, bool isSeller, OrderSearchRequest request, CancellationToken ct = default)
        {
            var result = await _orderRepo.GetPagedByUserAsync(userId, isSeller, request, ct);
            return Result<PagedResult<OrderListItemDto>>.Success(result);
        }

        public async Task<Result<OrderDetailDto>> GetDetailAsync(Guid orderId, Guid userId, CancellationToken ct = default)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, ct);
            if (order == null)
                return Result<OrderDetailDto>.Fail(new Error("Order.NotFound", "Không tìm thấy đơn hàng."));

            var authResult = await CheckOwnershipAsync(order.AgreementId, userId, ct);
            if (!authResult.IsSuccess)
                return Result<OrderDetailDto>.Fail(authResult.Error);

            var detail = await _orderRepo.GetDetailWithRelationsAsync(orderId, userId, ct);
            if (detail == null)
                return Result<OrderDetailDto>.Fail(new Error("Order.NotFound", "Không tìm thấy đơn hàng."));

            return Result<OrderDetailDto>.Success(detail);
        }

        public async Task<Result<order>> GetByAgreementAsync(Guid agreementId, Guid userId, CancellationToken ct = default)
        {
            var authResult = await CheckOwnershipAsync(agreementId, userId, ct);
            if (!authResult.IsSuccess)
                return Result<order>.Fail(authResult.Error);

            var order = await _orderRepo.GetByAgreementIdAsync(agreementId, ct);
            if (order == null)
                return Result<order>.Fail(new Error("Order.NotFound", "Thỏa thuận chưa phát sinh đơn hàng (chưa thanh toán thành công)."));

            return Result<order>.Success(order);
        }

        public async Task<Result<OrderConfirmationResponseDto>> ConfirmHandoverAsync(
            Guid orderId, Guid sellerId, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.AgreementNotFound);
                }

                if (agreement.SellerId != sellerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                // Order đã hoàn thành thì không ghi lại timestamp.
                if (order.OrderStatus == (int)OrderStatus.Completed)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderConfirmationResponseDto>.Success(new OrderConfirmationResponseDto
                    {
                        OrderId = order.OrderId,
                        OrderStatus = order.OrderStatus,
                        SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                        BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                        CompletedAt = order.CompletedAt,
                        CompletionSource = order.CompletionSource.HasValue
                            ? (OrderCompletionSource?)order.CompletionSource.Value
                            : null
                    });
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.InvalidStatus);
                }

                var deliveryMethodResult = ResolveDeliveryMethod(agreement);

                if (!deliveryMethodResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(deliveryMethodResult.Error!);
                }

                var deliveryMethod = deliveryMethodResult.Data;

                // Seller chỉ cần thao tác "Đã bàn giao" đối với giao nhận trực tiếp.
                if (deliveryMethod != DeliveryMethod.BuyerPickUp &&
                    deliveryMethod != DeliveryMethod.SellerDelivers)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.DirectHandoverOnly);
                }

                var collectionAppointment = await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                    agreement.AgreementId,
                    AppointmentType.Collection,
                ct);

                if (collectionAppointment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.CollectionAppointmentNotFound);
                }

                // Direct Collection: hai bên phải thực sự check-in tại lịch hẹn trước.
                if (!collectionAppointment.BuyerCheckAt.HasValue ||
                    !collectionAppointment.SellerCheckAt.HasValue)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.BothCheckInRequired);
                }

                // Idempotent: Seller bấm lại thì không đổi timestamp cũ.
                if (!order.SellerHandoverConfirmedAt.HasValue)
                {
                    var now = DateTime.UtcNow;

                    order.SellerHandoverConfirmedAt = now;
                    order.UpdatedAt = now;

                    await _orderRepo.UpdateAsync(order, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<OrderConfirmationResponseDto>.Success(new OrderConfirmationResponseDto
                {
                    OrderId = order.OrderId,
                    OrderStatus = order.OrderStatus,
                    SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                    BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                    CompletedAt = order.CompletedAt,
                    CompletionSource = order.CompletionSource.HasValue
                        ? (OrderCompletionSource?)order.CompletionSource.Value
                        : null
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<OrderConfirmationResponseDto>> ConfirmReceivedAsync(
    Guid orderId, Guid buyerId, CancellationToken ct = default)
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var order = await _orderRepo.GetByIdForUpdateAsync(orderId, ct);

                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.NotFound);
                }

                var agreement = await _agreementRepo.GetByIdAsync(order.AgreementId, ct);

                if (agreement == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.AgreementNotFound);
                }

                if (agreement.BuyerId != buyerId)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.Forbidden);
                }

                // Buyer bấm lại sau khi Order đã Completed.
                // Không ghi lại CompletedAt / BuyerReceivedConfirmedAt.
                if (order.OrderStatus == (int)OrderStatus.Completed)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return Result<OrderConfirmationResponseDto>.Success(new OrderConfirmationResponseDto
                    {
                        OrderId = order.OrderId,
                        OrderStatus = order.OrderStatus,
                        SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                        BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                        CompletedAt = order.CompletedAt,
                        CompletionSource = order.CompletionSource.HasValue
                            ? (OrderCompletionSource?)order.CompletionSource.Value
                            : null
                    });
                }

                if (order.OrderStatus != (int)OrderStatus.Processing)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.InvalidStatus);
                }

                var deliveryMethodResult = ResolveDeliveryMethod(agreement);

                if (!deliveryMethodResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(deliveryMethodResult.Error!);
                }

                var deliveryMethod = deliveryMethodResult.Data;

                // GHN: chỉ được confirm khi GHN đã báo giao thành công.
                if (deliveryMethod == DeliveryMethod.GhnDelivery)
                {
                    var shipment = await _shipmentRepo.GetByOrderIdAsync(order.OrderId, ct);

                    if (shipment == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.ShipmentNotFound);
                    }

                    if (shipment.ShipmentStatus != ShipmentStatus.Delivered ||
                        !shipment.DeliveredAt.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.ShipmentNotDelivered);
                    }
                }
                // Direct: hai bên phải check-in Collection.
                else if (deliveryMethod == DeliveryMethod.BuyerPickUp ||
                         deliveryMethod == DeliveryMethod.SellerDelivers)
                {
                    var collectionAppointment = await _appointmentRepo.GetByAgreementIdAndTypeAsync(
                        agreement.AgreementId,
                        AppointmentType.Collection,
                        ct);

                    if (collectionAppointment == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.CollectionAppointmentNotFound);
                    }

                    if (!collectionAppointment.BuyerCheckAt.HasValue ||
                        !collectionAppointment.SellerCheckAt.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.BothCheckInRequired);
                    }
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return Result<OrderConfirmationResponseDto>.Fail(OrderErrors.DeliveryMethodMissing);
                }

                var now = DateTime.UtcNow;

                // Chỉ ghi thao tác thực sự của Buyer.
                // Không tự set SellerHandoverConfirmedAt nếu Seller chưa từng bấm.
                order.BuyerReceivedConfirmedAt = now;

                // Buyer xác nhận đã nhận hàng là đủ căn cứ đóng Order.
                order.OrderStatus = (int)OrderStatus.Completed;
                order.CompletedAt = now;
                order.CompletionSource = (int)OrderCompletionSource.BuyerConfirmed;
                order.UpdatedAt = now;

                await _orderRepo.UpdateAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return Result<OrderConfirmationResponseDto>.Success(new OrderConfirmationResponseDto
                {
                    OrderId = order.OrderId,
                    OrderStatus = order.OrderStatus,
                    SellerHandoverConfirmedAt = order.SellerHandoverConfirmedAt,
                    BuyerReceivedConfirmedAt = order.BuyerReceivedConfirmedAt,
                    CompletedAt = order.CompletedAt,
                    CompletionSource = order.CompletionSource.HasValue
                        ? (OrderCompletionSource?)order.CompletionSource.Value
                        : null
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        private static Result<DeliveryMethod> ResolveDeliveryMethod(agreement_form agreement)
        {
            if (string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);

            try
            {
                var details = JsonSerializer.Deserialize<AgreementDetailsDto>(
                    agreement.AgreementDetailsJsonb,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (details?.DeliveryMethod == null ||
                    details.DeliveryMethod == DeliveryMethod.Unknown)
                {
                    return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);
                }

                return Result<DeliveryMethod>.Success(details.DeliveryMethod.Value);
            }
            catch (JsonException)
            {
                return Result<DeliveryMethod>.Fail(OrderErrors.DeliveryMethodMissing);
            }
        }
        private async Task<Result<bool>> CheckOwnershipAsync(Guid agreementId, Guid userId, CancellationToken ct)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<bool>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != userId && agreement.SellerId != userId)
                return Result<bool>.Fail(new Error("Auth.Forbidden", "Bạn không có quyền xem đơn hàng này."));

            return Result<bool>.Success(true);
        }
    }
}
