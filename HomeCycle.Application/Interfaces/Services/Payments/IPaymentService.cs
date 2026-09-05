using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Payments
{
    public interface IPaymentService
    {
        Task<Result<string>> GeneratePayOSCheckoutUrlAsync(Guid agreementId, Guid payerId, string returnUrl, string cancelUrl, CancellationToken ct = default);
        Task<Result<bool>> HandlePaymentWebhookAsync(string webhookBody, CancellationToken ct = default);
        Task<Result<bool>> ExecuteWalletPaymentAsync(Guid agreementId, Guid payerId, CancellationToken ct = default);
        Task<Result<string>> SyncPaymentStatusAsync(Guid agreementId, Guid payerId, CancellationToken ct = default);
        Task<Result<PagedResult<PaymentHistoryResponseDto>>> GetMyPaymentHistoryAsync(Guid userId, PaymentHistorySearchRequest request, CancellationToken ct = default);
        Task<Result<bool>> RefundOrderHeldAmountAsync(order order, agreement_form agreement, decimal amount, CancellationToken ct = default);
        Task<Result<decimal>> RefundAllRemainingOrderHeldAmountAsync(
            order order,
            agreement_form agreement,
            CancellationToken ct = default);
    }
}
