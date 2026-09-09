using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Payments
{
    public interface IOrderSettlementService
    {
        Task<Result<string>> GeneratePayOsCheckoutUrlAsync(
            Guid paymentId,
            Guid payerId,
            string returnUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default);

        Task<Result<PaymentStatusResponseDto>> ExecuteWalletAsync(
            Guid paymentId,
            Guid payerId,
            CancellationToken cancellationToken = default);

        Task<Result<PaymentStatusResponseDto>> SyncPayOsStatusAsync(
            Guid paymentId,
            Guid payerId,
            CancellationToken cancellationToken = default);

        Task<Result> CompletePayOsAsync(
            string payOsOrderCode,
            string payOsTransactionId,
            CancellationToken cancellationToken = default);
    }

}
