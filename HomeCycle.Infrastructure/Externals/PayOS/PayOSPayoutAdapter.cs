using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.Interfaces.Externals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V1.Payouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.PayOS
{
    public class PayOSPayoutAdapter : IPayoutGatewayService
    {
        private readonly PayOSClient _payoutClient;
        private readonly ILogger<PayOSPayoutAdapter> _logger;

        public PayOSPayoutAdapter(IOptions<PayOSPayoutSettings> options, ILogger<PayOSPayoutAdapter> logger)
        {
            var settings = options.Value;
            _payoutClient = new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey);
            _logger = logger;
        }

        public async Task<Result<GatewayPayoutResponse>> CreatePayoutAsync(
    GatewayPayoutRequest request, CancellationToken ct = default)
        {
            try
            {
                var result = await _payoutClient.Payouts.CreateAsync(new PayoutRequest
                {
                    ReferenceId = request.ReferenceId,
                    Amount = request.Amount,
                    Description = request.Description,
                    ToBin = request.ToBin,
                    ToAccountNumber = request.ToAccountNumber
                });

                return Result<GatewayPayoutResponse>.Success(new GatewayPayoutResponse
                {
                    PayoutId = result.Id,
                    ApprovalState = result.ApprovalState.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi payOS Payout cho ReferenceId {ReferenceId}", request.ReferenceId);
                return Result<GatewayPayoutResponse>.Fail(new Error("Payout.CreateFailed", ex.Message));
            }
        }

        public async Task<Result<GatewayPayoutStatusResponse>> GetPayoutStatusAsync(
            string referenceId, CancellationToken ct = default)
        {
            try
            {
                var payout = await _payoutClient.Payouts.GetAsync(referenceId);
                var transaction = payout.Transactions.FirstOrDefault();

                return Result<GatewayPayoutStatusResponse>.Success(new GatewayPayoutStatusResponse
                {
                    PayoutId = payout.Id,
                    ApprovalState = payout.ApprovalState.ToString().ToUpperInvariant(),
                    TransactionState = transaction?.State.ToString().ToUpperInvariant(),
                    FailureReason = transaction?.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy trạng thái payout {ReferenceId}", referenceId);
                return Result<GatewayPayoutStatusResponse>.Fail(new Error("Payout.GetStatusFailed", ex.Message));
            }
        }
    }
}
