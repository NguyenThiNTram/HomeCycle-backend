using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;

        public OrderService(IOrderRepository orderRepo, IAgreementFormRepository agreementRepo)
        {
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
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
