using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Externals
{
    public interface IPaymentGatewayService
    {
        Task<Result<GatewayPaymentResponse>> CreatePaymentLinkAsync(GatewayPaymentRequest request, CancellationToken ct = default);

        Task<Result<GatewayWebhookResult>> VerifyAndParseWebhookAsync(string webhookBody);
        Task<Result<GatewayPaymentStatusResponse>> GetPaymentStatusAsync(string payOSOrderCode, CancellationToken ct = default);
    }
}

