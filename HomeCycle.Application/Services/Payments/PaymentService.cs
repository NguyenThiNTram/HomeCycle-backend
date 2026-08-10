using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        // Tech Lead Note: Hằng số thay cho Magic Numbers. 
        // Đặt ở đây để dễ dàng thay đổi theo Business rule mà không phải lục tìm trong logic.
        private const decimal DEPOSIT_RATE = 0.20m;
        private static readonly TimeSpan PAYMENT_TTL = TimeSpan.FromMinutes(15);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentGatewayService _gatewayService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPaymentTransactionRepository _paymentTxRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _walletTxRepo;
        private readonly IWalletLedgerRepository _ledgerRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ICollectionAppointmentRepository _collectionRepo;
        private readonly IInspectionAppointmentRepository _inspectionRepo;
        private readonly IPostRepository _postRepo;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IPaymentGatewayService gatewayService,
            IPaymentRepository paymentRepo,
            IPaymentTransactionRepository paymentTxRepo,
            IOrderRepository orderRepo,
            IAgreementFormRepository agreementRepo,
            IWalletRepository walletRepo,
            IWalletTransactionRepository walletTxRepo,
            IWalletLedgerRepository ledgerRepo,
            IAppointmentRepository appointmentRepo,
            ICollectionAppointmentRepository collectionRepo,
            IInspectionAppointmentRepository inspectionRepo,
            IPostRepository postRepo,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _gatewayService = gatewayService;
            _paymentRepo = paymentRepo;
            _paymentTxRepo = paymentTxRepo;
            _orderRepo = orderRepo;
            _agreementRepo = agreementRepo;
            _walletRepo = walletRepo;
            _walletTxRepo = walletTxRepo;
            _ledgerRepo = ledgerRepo;
            _appointmentRepo = appointmentRepo;
            _collectionRepo = collectionRepo;
            _inspectionRepo = inspectionRepo;
            _postRepo = postRepo;
            _logger = logger;
        }

        public async Task<Result<string>> GeneratePayOSCheckoutUrlAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<string>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != payerId)
                return Result<string>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền thanh toán thỏa thuận này."));

            // Chỉ cho tạo checkout link khi Agreement đang thật sự chờ thanh toán.
            if (agreement.AgreementStatus != (int)AgreementStatus.Awaiting_Payment)
                return Result<string>.Fail(new Error("Agreement.InvalidStatus", "Thỏa thuận không ở trạng thái chờ thanh toán."));

            // Tái sử dụng payment Pending còn hạn thay vì tạo mới liên tục.
            var existingPending = await _paymentRepo.GetLatestPendingByAgreementAsync(agreementId, ct);
            if (existingPending != null)
            {
                if (existingPending.ExpiredAt.HasValue && existingPending.ExpiredAt.Value > DateTime.UtcNow)
                {
                    var existingTx = await _paymentTxRepo.GetLatestByPaymentIdAsync(existingPending.PaymentId, ct);
                    if (existingTx != null && !string.IsNullOrEmpty(existingTx.CheckoutUrl))
                        return Result<string>.Success(existingTx.CheckoutUrl);
                }
                else
                {
                    // Hết hạn -> đánh dấu Expired để mở đường tạo payment mới.
                    existingPending.PaymentStatus = (int)PaymentStatus.Expired;
                    await _paymentRepo.UpdateAsync(existingPending, ct);
                }
            }

            AgreementDetailsDto? details;
            try
            {
                details = ParseAgreementDetails(agreement, agreementId);
            }
            catch (JsonException)
            {
                return Result<string>.Fail(new Error("Data.InvalidFormat", "Dữ liệu JSONB cấu hình thỏa thuận bị lỗi."));
            }

            var calc = CalculatePaymentAmount(agreement, details);
            agreement.PaymentType = calc.PaymentType;

            long orderCode = 0;
            const int maxOrderCodeAttempts = 5;
            for (int attempt = 0; attempt < maxOrderCodeAttempts; attempt++)
            {
                orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 900000000 + 100000000;
                if (!await _paymentTxRepo.ExistsByPayOSOrderCodeAsync(orderCode.ToString(), ct))
                    break;
                if (attempt == maxOrderCodeAttempts - 1)
                    return Result<string>.Fail(new Error("Payment.OrderCodeConflict", "Không thể khởi tạo mã đơn hàng, vui lòng thử lại."));
                await Task.Delay(5, ct); // đẩy timestamp sang millisecond khác
            }

            var gatewayRequest = new GatewayPaymentRequest
            {
                OrderCode = orderCode,
                Amount = (int)calc.AmountToPay,
                Description = $"TT AGREE {agreementId.ToString().Substring(0, 6)}",
                BuyerName = "Buyer",
                BuyerEmail = "buyer@homecycle.vn"
            };

            var gatewayResult = await _gatewayService.CreatePaymentLinkAsync(gatewayRequest, ct);
            if (!gatewayResult.IsSuccess)
                return Result<string>.Fail(gatewayResult.Error);

            var paymentId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var payment = new payment
            {
                PaymentId = paymentId,
                AgreementId = agreement.AgreementId,
                PayerId = payerId,
                PaymentType = agreement.PaymentType,
                PaymentMethod = (int)PaymentMethod.PayOS,
                Amount = calc.AmountToPay,
                Description = "Thanh toan qua PayOS",
                PaymentStatus = (int)PaymentStatus.Pending,
                CreatedAt = now,
                ExpiredAt = now.Add(PAYMENT_TTL)
            };

            var paymentTx = new payment_transaction
            {
                PaymentTransactionId = Guid.NewGuid(),
                PaymentId = paymentId,
                UserId = payerId,
                PayOSOrderCode = orderCode.ToString(),
                PayOSPaymentLinkId = gatewayResult.Data.PaymentLinkId,
                CheckoutUrl = gatewayResult.Data.CheckoutUrl,
                PaymentTransactionStatus = (int)PaymentTransactionStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _agreementRepo.UpdateAsync(agreement, ct);
                await _paymentRepo.AddAsync(payment, ct);
                await _paymentTxRepo.AddAsync(paymentTx, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync();

                return Result<string>.Success(gatewayResult.Data.CheckoutUrl);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo payment link và lưu DB cho agreement {AgreementId}", agreementId);
                return Result<string>.Fail(new Error("Payment.CreateFailed", "Không thể khởi tạo thông tin thanh toán."));
            }
        }

        public async Task<Result<bool>> HandlePaymentWebhookAsync(string webhookBody, CancellationToken ct = default)
        {
            var verifyResult = await _gatewayService.VerifyAndParseWebhookAsync(webhookBody);
            if (!verifyResult.IsSuccess)
                return Result<bool>.Fail(verifyResult.Error);

            var payload = verifyResult.Data;
            if (payload.Status != "Success")
                return Result<bool>.Success(true);

            await ExecuteSuccessfulPaymentCoreAsync(payload.OrderCode.ToString(), payload.ReferenceTransactionId, ct);
            return Result<bool>.Success(true);
        }


        public async Task<Result<bool>> ExecuteWalletPaymentAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        {
            // 1. LẤY VÀ KIỂM TRA DỮ LIỆU CƠ BẢN
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<bool>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != payerId)
                return Result<bool>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền thanh toán."));

            // Guard chống trùng thanh toán: dựa trên AgreementStatus thay vì PaymentType
            // (PaymentType luôn có giá trị ngay khi tạo Agreement nên không dùng để check đã-thanh-toán được).
            if (agreement.AgreementStatus != (int)AgreementStatus.Awaiting_Payment)
                return Result<bool>.Fail(new Error("Agreement.InvalidStatus", "Thỏa thuận không ở trạng thái chờ thanh toán."));

            // 2. BÓC TÁCH JSONB VÀ TÍNH TOÁN DÒNG TIỀN (dùng chung CalculatePaymentAmount)
            AgreementDetailsDto? details;
            try
            {
                details = ParseAgreementDetails(agreement, agreementId);
            }
            catch (JsonException)
            {
                return Result<bool>.Fail(new Error("Data.InvalidFormat", "Dữ liệu JSONB bị lỗi."));
            }

            DateTime? inspectionDate = details?.InspectionDate;
            string? inspectionAddress = details?.InspectionAddress;
            DateTime? collectionDate = details?.CollectionDate;
            string? pickupAddress = details?.PickupAddress;
            string? deliveryAddress = details?.DeliveryAddress;
            string? deliveryMethodString = details?.DeliveryMethod?.ToString();

            var calc = CalculatePaymentAmount(agreement, details);
            decimal basePrice = calc.BasePrice;
            decimal amountToPay = calc.AmountToPay;
            agreement.PaymentType = calc.PaymentType;

            // 3. KIỂM TRA VÍ NGƯỜI MUA
            var buyerWallet = await _walletRepo.GetByUserIdAndTypeAsync(payerId, WalletTypeEnum.Personal, ct);
            if (buyerWallet == null || buyerWallet.AvailableBalance < amountToPay)
                return Result<bool>.Fail(new Error("Wallet.InsufficientBalance", "Số dư ví không đủ để thực hiện giao dịch."));

            var sellerWallet = await _walletRepo.GetByUserIdAndTypeAsync(agreement.SellerId, WalletTypeEnum.Personal, ct);
            if (sellerWallet == null)
                return Result<bool>.Fail(new Error("Wallet.SellerNotFound", "Không tìm thấy ví của người bán."));

            // 4. BẮT ĐẦU TRANSACTION CORE LÕI
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var paymentId = Guid.NewGuid();
                var orderId = Guid.NewGuid();
                var now = DateTime.UtcNow;

                // 4.1 Trừ tiền ví người mua (Dòng tiền ra)
                var buyerWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = buyerWallet.WalletId,
                    PaymentId = paymentId,
                    ReferenceId = orderId,
                    ReferenceType = (int)ReferenceType.Order,
                    TransactionType = (int)TransactionType.Wallet_Payment, // Thanh toán hàng hóa
                    Amount = -amountToPay, // Lưu ý: Số âm vì trừ tiền
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                var buyerLedger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = buyerWalletTx.WalletTransactionId,
                    WalletId = buyerWallet.WalletId,
                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Available,
                    Amount = amountToPay, // Ledger lưu số tuyệt đối
                    BalanceBefore = buyerWallet.AvailableBalance,
                    BalanceAfter = buyerWallet.AvailableBalance - amountToPay,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = orderId,
                    Description = $"Thanh toan don hang {orderId} tu vi",
                    CreatedAt = DateTime.UtcNow
                };
                buyerWallet.AvailableBalance -= amountToPay;
                buyerWallet.UpdatedAt = DateTime.UtcNow;

                // 4.2 Cộng tiền ví tạm giữ Escrow người bán (Dòng tiền vào)
                var sellerWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = sellerWallet.WalletId,
                    PaymentId = paymentId,
                    ReferenceId = orderId,
                    ReferenceType = (int)ReferenceType.Order,
                    TransactionType = (int)TransactionType.Escrow_Deposit,
                    Amount = amountToPay,
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                var sellerLedger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = sellerWalletTx.WalletTransactionId,
                    WalletId = sellerWallet.WalletId,
                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = amountToPay,
                    BalanceBefore = sellerWallet.HoldBalance,
                    BalanceAfter = sellerWallet.HoldBalance + amountToPay,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = orderId,
                    Description = $"Tam giu tien cho don hang {orderId}",
                    CreatedAt = DateTime.UtcNow
                };
                sellerWallet.HoldBalance += amountToPay;
                sellerWallet.UpdatedAt = DateTime.UtcNow;

                // 4.3 Khởi tạo thực thể Payment (Wallet-specific: Completed ngay lập tức, không qua gateway ngoài)
                var payment = new payment
                {
                    PaymentId = paymentId,
                    AgreementId = agreement.AgreementId,
                    PayerId = payerId,
                    PaymentType = agreement.PaymentType,
                    PaymentMethod = (int)PaymentMethod.Internal_Wallet, // Khác PayOS
                    Amount = amountToPay,
                    Description = "Thanh toan qua Vi noi bo",
                    PaymentStatus = (int)PaymentStatus.Completed, // Trạng thái Completed ngay lập tức
                    CreatedAt = now,
                    PaidAt = now
                };

                // 4.4 Hiện thực hóa Agreement -> Order/Appointment/trừ Quantity/Confirmed (dùng chung với PayOS)
                await FulfillAgreementAsync(agreement, basePrice, amountToPay, details, ct, orderIdOverride: orderId);

                // 4.5 Lưu Data
                await _walletRepo.UpdateAsync(buyerWallet, ct);
                await _walletRepo.UpdateAsync(sellerWallet, ct);
                await _walletTxRepo.AddAsync(buyerWalletTx, ct);
                await _ledgerRepo.AddAsync(buyerLedger, ct);
                await _walletTxRepo.AddAsync(sellerWalletTx, ct);
                await _ledgerRepo.AddAsync(sellerLedger, ct);
                await _paymentRepo.AddAsync(payment, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi hạch toán thanh toán ví nội bộ cho Agreement {AgreementId}", agreementId);
                return Result<bool>.Fail(new Error("WalletPayment.TransactionFailed", "Giao dịch thất bại do lỗi hệ thống."));
            }
        }

        //public async Task<Result<string>> SyncPaymentStatusAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        //{
        //    var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
        //    if (agreement == null)
        //        return Result<string>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

        //    if (agreement.BuyerId != payerId)
        //        return Result<string>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền xem trạng thái thanh toán này."));

        //    var pending = await _paymentRepo.GetLatestPendingByAgreementAsync(agreementId, ct);
        //    if (pending == null)
        //    {
        //        var currentStatus = agreement.AgreementStatus == (int)AgreementStatus.Confirmed
        //            ? PaymentStatus.Completed
        //            : PaymentStatus.Pending;
        //        return Result<string>.Success(currentStatus.ToString());
        //    }

        //    var tx = await _paymentTxRepo.GetLatestByPaymentIdAsync(pending.PaymentId, ct);
        //    if (tx == null)
        //        return Result<string>.Fail(new Error("Payment.TransactionNotFound", "Không tìm thấy giao dịch tương ứng."));

        //    var statusResult = await _gatewayService.GetPaymentStatusAsync(tx.PayOSOrderCode, ct);
        //    if (!statusResult.IsSuccess)
        //        return Result<string>.Fail(statusResult.Error);

        //    switch (statusResult.Data.Status)
        //    {
        //        case "PAID":
        //            // Webhook có thể bị delay/miss — chủ động fulfill luôn nếu phát hiện đã PAID thật.
        //            await ExecuteSuccessfulPaymentCoreAsync(tx.PayOSOrderCode, statusResult.Data.TransactionId ?? string.Empty, ct);
        //            return Result<string>.Success(PaymentStatus.Completed.ToString());

        //        case "CANCELLED":
        //            pending.PaymentStatus = (int)PaymentStatus.Cancelled;
        //            tx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Cancelled;
        //            tx.UpdatedAt = DateTime.UtcNow;
        //            await _paymentRepo.UpdateAsync(pending, ct);
        //            await _paymentTxRepo.UpdateAsync(tx, ct);

        //            return Result<string>.Success(PaymentStatus.Cancelled.ToString());

        //        case "PENDING":
        //        case "PROCESSING":
        //            if (pending.ExpiredAt.HasValue && pending.ExpiredAt.Value <= DateTime.UtcNow)
        //            {
        //                pending.PaymentStatus = (int)PaymentStatus.Expired;
        //                await _paymentRepo.UpdateAsync(pending, ct);
        //                return Result<string>.Success(PaymentStatus.Expired.ToString());
        //            }
        //            return Result<string>.Success(PaymentStatus.Pending.ToString());

        //        default:
        //            return Result<string>.Success(PaymentStatus.Pending.ToString());
        //    }
        //}


        //Helper
        private async Task ExecuteSuccessfulPaymentCoreAsync(string payOsOrderCode, string payOsTransactionId, CancellationToken ct)
        {
            var paymentTx = await _paymentTxRepo.GetByPayOSOrderCodeAsync(payOsOrderCode, ct);
            if (paymentTx == null || paymentTx.PaymentTransactionStatus == (int)PaymentTransactionStatus.Success)
                return; // Ngăn chặn Webhook gọi 2 lần (Idempotency)

            var payment = await _paymentRepo.GetByIdAsync(paymentTx.PaymentId, ct);
            var agreement = await _agreementRepo.GetByIdAsync(payment.AgreementId.Value, ct);
            var sellerWallet = await _walletRepo.GetByUserIdAndTypeAsync(agreement.SellerId, WalletTypeEnum.Personal, ct);

            AgreementDetailsDto? details;
            try
            {
                details = ParseAgreementDetails(agreement, agreement.AgreementId);
            }
            catch (JsonException)
            {
                _logger.LogError("Lỗi bóc tách Jsonb ở hàm ExecuteSuccessfulPaymentCoreAsync cho Agreement {AgreementId}", agreement.AgreementId);
                details = null;
            }

            decimal unitPrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
            decimal basePrice = unitPrice * Math.Max(agreement.Quantity, 1);
            decimal paidAmount = payment.Amount ?? 0;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Cập nhật trạng thái thanh toán (PayOS-specific)
                paymentTx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Success;
                paymentTx.PayOSTransactionId = payOsTransactionId;
                paymentTx.UpdatedAt = DateTime.UtcNow;

                payment.PaymentStatus = (int)PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;

                // 2. Hiện thực hóa Agreement -> Order/Appointment/trừ Quantity/Confirmed (dùng chung với Wallet)
                var fulfillment = await FulfillAgreementAsync(agreement, basePrice, paidAmount, details, ct);

                // 3. Hạch toán ví (Escrow Logic) — chỉ ghi nhận CHIỀU VÀO cho seller,
                // vì tiền buyer đã rời hệ thống qua PayOS, không qua ví nội bộ.
                var newWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = sellerWallet.WalletId,
                    PaymentId = payment.PaymentId,
                    ReferenceId = fulfillment.Order.OrderId,
                    ReferenceType = (int)ReferenceType.Order,
                    TransactionType = (int)TransactionType.Escrow_Deposit,
                    Amount = paidAmount,
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                var newLedger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = newWalletTx.WalletTransactionId,
                    WalletId = sellerWallet.WalletId,
                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = paidAmount,
                    BalanceBefore = sellerWallet.HoldBalance,
                    BalanceAfter = sellerWallet.HoldBalance + paidAmount,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = fulfillment.Order.OrderId,
                    Description = $"Tam giu tien cho don hang {fulfillment.Order.OrderId}",
                    CreatedAt = DateTime.UtcNow
                };

                sellerWallet.HoldBalance += paidAmount;
                sellerWallet.UpdatedAt = DateTime.UtcNow;

                // 4. Lưu vào Database
                await _paymentTxRepo.UpdateAsync(paymentTx, ct);
                await _paymentRepo.UpdateAsync(payment, ct);
                await _walletTxRepo.AddAsync(newWalletTx, ct);
                await _ledgerRepo.AddAsync(newLedger, ct);
                await _walletRepo.UpdateAsync(sellerWallet, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi hạch toán giao dịch webhook cho PayOS OrderCode {OrderCode}", payOsOrderCode);
                throw;
            }
        }
        private sealed class PaymentCalculation
        {
            public decimal AmountToPay { get; init; }
            public decimal BasePrice { get; init; }
            public int PaymentType { get; init; }
            public DeliveryMethod? DeliveryMethod { get; init; }
            public decimal ShippingFee { get; init; }
        }

        private PaymentCalculation CalculatePaymentAmount(agreement_form agreement, AgreementDetailsDto? details)
        {
            decimal unitPrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
            decimal basePrice = unitPrice * Math.Max(agreement.Quantity, 1);

            var deliveryMethod = details?.DeliveryMethod;
            decimal shippingFee = details?.EstimatedShippingFee ?? 0;

            decimal amountToPay;
            int paymentType;

            if (agreement.AgreementType == (int)AgreementType.Inspection)
            {
                amountToPay = basePrice * DEPOSIT_RATE;
                paymentType = (int)PaymentType.Deposit;
            }
            else if (deliveryMethod == DeliveryMethod.GhnDelivery)
            {
                amountToPay = basePrice;
                paymentType = (int)PaymentType.Full_Payment;
            }
            else
            {
                // BuyerPickUp / SellerDelivers / fallback: giữ nguyên loại thanh toán đã chốt lúc tạo Agreement.
                paymentType = agreement.PaymentType ?? (int)PaymentType.Full_Payment;
                decimal itemPay = paymentType == (int)PaymentType.Deposit ? basePrice * DEPOSIT_RATE : basePrice;
                amountToPay = deliveryMethod == DeliveryMethod.SellerDelivers ? itemPay + shippingFee : itemPay;
            }

            amountToPay = Math.Round(amountToPay, 0, MidpointRounding.AwayFromZero);

            return new PaymentCalculation
            {
                AmountToPay = amountToPay,
                BasePrice = basePrice,
                PaymentType = paymentType,
                DeliveryMethod = deliveryMethod,
                ShippingFee = shippingFee
            };
        }

        private AgreementDetailsDto? ParseAgreementDetails(agreement_form agreement, Guid agreementId)
        {
            if (string.IsNullOrEmpty(agreement.AgreementDetailsJsonb))
                return null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi parse AgreementDetailsJsonb cho Agreement {AgreementId}", agreementId);
                throw;
            }
        }

        private sealed class FulfillmentResult
        {
            public order Order { get; init; } = null!;
            public appointment Appointment { get; init; } = null!;
            public inspection_appointment? InspectionAppointment { get; init; }
            public collection_appointment? CollectionAppointment { get; init; }
            public post? Post { get; init; }
        }

        private async Task<FulfillmentResult> FulfillAgreementAsync(
            agreement_form agreement,
            decimal basePrice,
            decimal paidAmount,
            AgreementDetailsDto? details,
            CancellationToken ct,
            Guid? orderIdOverride = null)
        {
            var orderId = orderIdOverride ?? Guid.NewGuid();
            var order = new order
            {
                OrderId = orderId,
                AgreementId = agreement.AgreementId,
                PostId = agreement.PostId,
                Quantity = agreement.Quantity,
                OriginalTotalAmount = basePrice,
                FinalTotalAmount = basePrice,
                AmountPaid = paidAmount,
                AmountRemaining = basePrice - paidAmount > 0 ? basePrice - paidAmount : 0,
                PaymentStatus = agreement.PaymentType == (int)PaymentType.Deposit ? (int)PaymentStatus.Pending : (int)PaymentStatus.Completed,
                OrderStatus = (int)OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };


            var appointmentType = agreement.AgreementType == (int)AgreementType.Inspection
                ? AppointmentType.Inspection
                : AppointmentType.Collection;

            var appointmentId = Guid.NewGuid();
            var appointment = new appointment
            {
                AppointmentId = appointmentId,
                AgreementId = agreement.AgreementId,
                AppointmentType = (int)appointmentType,
                AppointmentStatus = (int)AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
                // UpdatedAt: để null.
            };

            inspection_appointment? inspectionAppt = null;
            collection_appointment? collectionAppt = null;

            if (appointmentType == AppointmentType.Inspection)
            {
                inspectionAppt = new inspection_appointment
                {
                    InspectionAppointmentId = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    InspectionAddress = details?.InspectionAddress ?? string.Empty,
                    InspectionDate = details?.InspectionDate ?? DateTime.UtcNow.AddDays(1)
                };
                await _inspectionRepo.AddAsync(inspectionAppt, ct);
            }
            else
            {
                collectionAppt = new collection_appointment
                {
                    CollectionAppointmentId = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    CollectionDate = details?.CollectionDate,
                    PickupAddress = details?.PickupAddress,
                    DeliveryAddress = details?.DeliveryAddress,
                    DeliveryMethod = details?.DeliveryMethod?.ToString()
                };
                await _collectionRepo.AddAsync(collectionAppt, ct);
            }

            await _appointmentRepo.AddAsync(appointment, ct);
            await _orderRepo.AddAsync(order, ct);

            // Trừ số lượng còn lại của Post.
            post? post = await _postRepo.GetByIdAsync(agreement.PostId, ct);
            if (post != null)
            {
                post.RemainingQuantity = Math.Max(post.RemainingQuantity - agreement.Quantity, 0);
                if (post.RemainingQuantity <= 0)
                    post.Status = PostStatus.Closed;
                await _postRepo.UpdateAsync(post, ct);
            }

            agreement.AgreementStatus = (int)AgreementStatus.Confirmed;
            await _agreementRepo.UpdateAsync(agreement, ct);

            return new FulfillmentResult
            {
                Order = order,
                Appointment = appointment,
                InspectionAppointment = inspectionAppt,
                CollectionAppointment = collectionAppt,
                Post = post
            };
        }
    }
}