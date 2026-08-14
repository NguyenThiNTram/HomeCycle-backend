using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.Interfaces.Externals;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace HomeCycle.Infrastructure.Externals.PayOS
{
    public class PayOSGatewayAdapter : IPaymentGatewayService
    {
        private readonly PayOSClient _payOSClient;
        private readonly PayOSSettings _settings;

        public PayOSGatewayAdapter(IOptions<PayOSSettings> options)
        {
            _settings = options.Value;
            _payOSClient = new PayOSClient(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);

        }

        public async Task<Result<GatewayPaymentResponse>> CreatePaymentLinkAsync(GatewayPaymentRequest request, CancellationToken ct = default)
        {
            try
            {
                var paymentRequest = new CreatePaymentLinkRequest
                {
                    OrderCode = request.OrderCode,
                    Amount = request.Amount,
                    Description = request.Description,
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl,
                    BuyerName = request.BuyerName,
                    BuyerEmail = request.BuyerEmail
                };

                // Gọi hàm tạo link qua property PaymentRequests
                var paymentLink = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);

                return Result<GatewayPaymentResponse>.Success(new GatewayPaymentResponse
                {
                    CheckoutUrl = paymentLink.CheckoutUrl,
                    PaymentLinkId = paymentLink.PaymentLinkId ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return Result<GatewayPaymentResponse>.Fail(new Error("PayOS.CreateLinkFailed", $"Lỗi tạo link thanh toán v2: {ex.Message}"));
            }
        }

        public async Task<Result<GatewayWebhookResult>> VerifyAndParseWebhookAsync(string webhookBody)
        {
            try
            {
                // 1. Ép kiểu raw body thành Webhook payload của PayOS v2.1.0
                var webhookPayload = JsonSerializer.Deserialize<Webhook>(webhookBody);

                if (webhookPayload == null)
                    return Result<GatewayWebhookResult>.Fail(new Error("PayOS.ParseError", "Không thể parse chuỗi JSON từ webhook."));

                // 2. Gọi hàm SDK để xác thực chữ ký (Signature) - Phòng chống Hacker ném payload giả
                WebhookData verifiedData = await _payOSClient.Webhooks.VerifyAsync(webhookPayload);


                string mappedStatus = verifiedData.Code switch
                {
                    "00" => "Success",
                    // Ví dụ nếu tương lai có mã lỗi
                    // "01" => "Failed", 
                    _ => "Unknown"
                };


                var resultDto = new GatewayWebhookResult
                {
                    OrderCode = verifiedData.OrderCode,
                    Amount = (int)verifiedData.Amount,
                    Status = mappedStatus,
                    ReferenceTransactionId = verifiedData.Reference ?? string.Empty,
                    Description = verifiedData.Description ?? string.Empty
                };

                return Result<GatewayWebhookResult>.Success(resultDto);
            }
            catch (Exception ex)
            {

                return Result<GatewayWebhookResult>.Fail(new Error("PayOS.WebhookInvalid", $"Dữ liệu Webhook không hợp lệ hoặc sai chữ ký: {ex.Message}"));
            }
        }
        public async Task<Result<GatewayPaymentStatusResponse>> GetPaymentStatusAsync(string payOSOrderCode, CancellationToken ct = default)
        {
            try
            {
                var paymentLink = await _payOSClient.PaymentRequests.GetAsync(payOSOrderCode);

                if (paymentLink == null)
                    return Result<GatewayPaymentStatusResponse>.Fail(new Error("PayOS.NotFound", "Không tìm thấy đơn hàng trên PayOS."));

                return Result<GatewayPaymentStatusResponse>.Success(new GatewayPaymentStatusResponse
                {
                    // Chuẩn hóa về UPPERCASE bất kể SDK trả string hay enum (enum.ToString() thường
                    // là PascalCase "Paid"/"Pending", trong khi API gốc + switch-case ở PaymentService
                    // dùng UPPERCASE "PAID"/"PENDING" — lệch case khiến switch luôn rơi vào default.
                    Status = paymentLink.Status.ToString().ToUpperInvariant(),
                    TransactionId = paymentLink.Transactions?.LastOrDefault()?.Reference
                });
            }
            catch (Exception ex)
            {
                return Result<GatewayPaymentStatusResponse>.Fail(new Error("PayOS.GetStatusFailed", $"Lỗi khi lấy trạng thái thanh toán từ PayOS: {ex.Message}"));
            }
        }


    }
}
