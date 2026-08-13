using HomeCycle.Application.Commons.Results;
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
    }
}
