using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Orders
{
    public interface IOrderService
    {

        Task<Result<PagedResult<OrderListItemDto>>> GetMyOrdersAsync(
            Guid userId, bool isSeller, OrderSearchRequest request, CancellationToken ct = default);
        Task<Result<OrderDetailDto>> GetDetailAsync(Guid orderId, Guid userId, CancellationToken ct = default);
        //Task<Result<order>> GetByAgreementAsync(Guid agreementId, Guid userId, CancellationToken ct = default);
        Task<Result<OrderConfirmationResponseDto>> ConfirmHandoverAsync(Guid orderId, Guid sellerId, CancellationToken ct = default);

        Task<Result<OrderConfirmationResponseDto>> ConfirmReceivedAsync(Guid orderId, Guid buyerId, CancellationToken ct = default);
        Task<Result<OrderReferenceDto>> GetByAgreementAsync(Guid agreementId, Guid userId, CancellationToken ct = default);
        Task<Result<OrderCancellationResponseDto>> CancelAfterRejectedInspectionAsync(Guid orderId, Guid userId, CancellationToken ct = default);
    }
}
