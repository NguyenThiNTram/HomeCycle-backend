using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.Interfaces.Externals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
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
            GatewayPayoutRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var payoutRequest = new PayoutRequest
                {
                    ReferenceId = request.ReferenceId,
                    Amount = request.Amount,
                    Description = request.Description,
                    ToBin = request.ToBin,
                    ToAccountNumber = request.ToAccountNumber
                };

                var requestOptions = new RequestOptions<Payout>
                {
                    CancellationToken = ct
                };

                var result = await _payoutClient.Payouts.CreateAsync(
                    payoutRequest,
                    request.ReferenceId,
                    requestOptions);

                return Result<GatewayPayoutResponse>.Success(
                    new GatewayPayoutResponse
                    {
                        PayoutId = result.Id,
                        ApprovalState =
                            PayoutApprovalStateConverter.ToSerializedString(
                                result.ApprovalState)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi gọi payOS Payout cho ReferenceId {ReferenceId}",
                    request.ReferenceId);

                return Result<GatewayPayoutResponse>.Fail(
                    new Error("Payout.CreateFailed", ex.Message));
            }
        }

        public async Task<Result<GatewayPayoutStatusResponse>> GetPayoutStatusAsync(
            string referenceId,
            CancellationToken ct = default)
        {
            try
            {
                var requestOptions = new RequestOptions
                {
                    CancellationToken = ct
                };

                var page = await _payoutClient.Payouts.ListAsync(
                    new GetPayoutListParam
                    {
                        ReferenceId = referenceId,
                        Limit = 1,
                        Offset = 0
                    },
                    requestOptions);

                var payout = page.Data.FirstOrDefault(
                    x => string.Equals(
                        x.ReferenceId,
                        referenceId,
                        StringComparison.Ordinal));

                if (payout == null)
                {
                    return Result<GatewayPayoutStatusResponse>.Fail(
                        new Error(
                            "Payout.NotFound",
                            "Không tìm thấy lệnh chi tương ứng."));
                }

                var transaction = payout.Transactions.FirstOrDefault();

                return Result<GatewayPayoutStatusResponse>.Success(
                    new GatewayPayoutStatusResponse
                    {
                        PayoutId = payout.Id,
                        ApprovalState =
                            PayoutApprovalStateConverter.ToSerializedString(
                                payout.ApprovalState),
                        TransactionState = transaction == null
                            ? null
                            : PayoutTransactionStateConverter.ToSerializedString(
                                transaction.State),
                        FailureReason = transaction?.ErrorMessage
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi lấy trạng thái payout theo ReferenceId {ReferenceId}",
                    referenceId);

                return Result<GatewayPayoutStatusResponse>.Fail(
                    new Error("Payout.GetStatusFailed", ex.Message));
            }
        }
    }
}
