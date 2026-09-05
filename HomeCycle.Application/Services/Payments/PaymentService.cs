using FluentValidation;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.GHN;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Repositories.Posts;
using HomeCycle.Application.Interfaces.Repositories.Shipments;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Application.Interfaces.Services.Payments;
using HomeCycle.Application.Interfaces.Services.PlatformPolicies;
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
        private const decimal DEPOSIT_RATE = 0.20m;
        private static readonly TimeSpan PAYMENT_TTL = TimeSpan.FromMinutes(15);

        // GHN create-order limits (đơn vị: gram / cm)
        private const int GhnMaxWeightGram = 1_600_000;
        private const int GhnMinDimensionCm = 1;
        private const int GhnMaxDimensionCm = 200;

        // Sai số cho phép khi so khớp số tiền (tránh fail do PayOS làm tròn)
        private const decimal AmountEpsilon = 0.01m;

        private static readonly HashSet<string> ValidGhnRequiredNotes = new(StringComparer.OrdinalIgnoreCase)
        {
            "CHOTHUHANG",
            "CHOXEMHANGKHONGTHU",
            "KHONGCHOXEMHANG"
        };

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

        private readonly IShipmentRepository _shipmentRepo;
        private readonly IGhnShipmentRepository _ghnShipmentRepo;
        private readonly IValidator<PayOSCheckoutRequest> _payOSCheckoutValidator;
        private readonly IPlatformPolicyProvider _platformPolicyProvider;

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
            ILogger<PaymentService> logger,
            IShipmentRepository shipmentRepository,
            IGhnShipmentRepository ghnShipmentRepository,
            IValidator<PayOSCheckoutRequest> payOSCheckoutValidator, 
            IPlatformPolicyProvider platformPolicyProvider)
        {
            _unitOfWork = unitOfWork;
            _gatewayService = gatewayService;
            _paymentRepo = paymentRepo;
            _paymentTxRepo = paymentTxRepo;
            _shipmentRepo = shipmentRepository;
            _ghnShipmentRepo = ghnShipmentRepository;
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
            _payOSCheckoutValidator = payOSCheckoutValidator;
            _platformPolicyProvider = platformPolicyProvider;
        }

        public async Task<Result<string>> GeneratePayOSCheckoutUrlAsync(Guid agreementId, Guid payerId, string returnUrl, string cancelUrl, CancellationToken ct = default)
        {
            var urlValidation = await _payOSCheckoutValidator.ValidateAsync(
                new PayOSCheckoutRequest { ReturnUrl = returnUrl, CancelUrl = cancelUrl }, ct);
            if (!urlValidation.IsValid)
            {
                var msg = string.Join(" ", urlValidation.Errors.Select(e => e.ErrorMessage));
                return Result<string>.Fail(new Error("Payment.InvalidRedirectUrl", msg));
            }

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

            if (details?.EstimatedShippingFee is < 0)
                return Result<string>.Fail(new Error("Payment.InvalidShippingFee", "Phí vận chuyển không được nhỏ hơn 0."));

            if (details?.DeliveryMethod == DeliveryMethod.GhnDelivery
                && details?.EstimatedShippingFee is null)
            {
                return Result<string>.Fail(new Error(
                    "Payment.GhnShippingFeeMissing",
                    "Chưa có phí vận chuyển GHN. Vui lòng tính lại phí giao hàng trước khi thanh toán."));
            }

            var calc = CalculatePaymentAmount(agreement, details);

            if (calc.BasePrice <= 0 || calc.AmountToPay <= 0 || calc.AmountToPay > int.MaxValue)
            {
                return Result<string>.Fail(new Error(
                    "Payment.InvalidAmount",
                    "Số tiền thanh toán không hợp lệ hoặc vượt quá giới hạn cho phép."));
            }

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
                BuyerEmail = "buyer@homecycle.vn",
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var gatewayResult = await _gatewayService.CreatePaymentLinkAsync(gatewayRequest, ct);
            if (!gatewayResult.IsSuccess)
                return Result<string>.Fail(gatewayResult.Error);

            if (gatewayResult.Data == null || string.IsNullOrWhiteSpace(gatewayResult.Data.CheckoutUrl))
            {
                return Result<string>.Fail(new Error(
                    "Payment.InvalidGatewayResponse",
                    "PayOS không trả về đường dẫn thanh toán hợp lệ."));
            }

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

            if (details?.EstimatedShippingFee is < 0)
                return Result<bool>.Fail(new Error("Payment.InvalidShippingFee", "Phí vận chuyển không được nhỏ hơn 0."));

            if (details?.DeliveryMethod == DeliveryMethod.GhnDelivery
                && details?.EstimatedShippingFee is null)
            {
                return Result<bool>.Fail(new Error(
                    "Payment.GhnShippingFeeMissing",
                    "Chưa có phí vận chuyển GHN. Vui lòng tính lại phí giao hàng trước khi thanh toán."));
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
            decimal shippingFee = calc.ShippingFee;
            // ✅ THÊM
            decimal holdAmount = details?.DeliveryMethod == DeliveryMethod.GhnDelivery
                ? basePrice
                : amountToPay;

            if (basePrice <= 0 || amountToPay <= 0)
                return Result<bool>.Fail(new Error("Payment.InvalidAmount", "Số tiền thanh toán không hợp lệ."));

            agreement.PaymentType = calc.PaymentType;
            bool needsSystemLedger = details?.DeliveryMethod == DeliveryMethod.GhnDelivery && shippingFee > 0;

            // TRANSACTION CORE LÕI
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                wallet buyerWallet = null!;
                wallet sellerWallet = null!;
                wallet_transaction? systemWalletTx = null;
                wallet_ledger? buyerLedgerForSystem = null;
                wallet_ledger? systemLedger = null;

                // Kỹ thuật Deterministic Locking: Khóa theo thứ tự GUID để chống Deadlock
                if (string.Compare(payerId.ToString(), agreement.SellerId.ToString(), StringComparison.Ordinal) < 0)
                {
                    buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(payerId, ct);
                    sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.SellerId, ct);
                }
                else
                {
                    sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.SellerId, ct);
                    buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(payerId, ct);
                }

                // Validate Ví bên trong Transaction
                if (buyerWallet == null)
                    return Result<bool>.Fail(new Error("Wallet.BuyerNotFound", "Không tìm thấy ví của người mua."));

                if (sellerWallet == null)
                    return Result<bool>.Fail(new Error("Wallet.SellerNotFound", "Không tìm thấy ví của người bán."));

                if (buyerWallet.AvailableBalance < amountToPay)
                    return Result<bool>.Fail(new Error("Wallet.InsufficientBalance", "Số dư ví không đủ để thực hiện giao dịch."));

                wallet? systemWallet = null;
                if (needsSystemLedger)
                {
                    // Khóa ví System cuối cùng
                    systemWallet = await _walletRepo.GetSystemWalletForUpdateAsync(SystemWalletPurpose.Shipping_Escrow, ct);
                    if (systemWallet == null)
                        return Result<bool>.Fail(new Error("Wallet.SystemWalletNotFound", "Không tìm thấy ví hệ thống để nhận phí vận chuyển."));
                }

                var paymentId = Guid.NewGuid();
                var orderId = Guid.NewGuid();
                var now = DateTime.UtcNow;


                // HẠCH TOÁN 1: TIỀN VỀ NGƯỜI BÁN (Goods / Goods + Shipping)
                var sellerWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    FromWalletId = buyerWallet.WalletId,
                    ToWalletId = sellerWallet.WalletId,
                    PaymentId = paymentId,
                    ReferenceId = orderId,
                    ReferenceType = (int)ReferenceType.Order,
                    TransactionType = (int)TransactionType.Wallet_Payment, // Thanh toán từ ví
                    Amount = holdAmount, 
                    WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                    CreatedAt = now
                };

                // Ledger 1.1: Trừ tiền Buyer (Out)
                var buyerLedgerForSeller = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = sellerWalletTx.WalletTransactionId,
                    WalletId = buyerWallet.WalletId,
                    Direction = (int)LedgerDirection.Out,
                    BalanceType = (int)BalanceType.Available,
                    Amount = holdAmount,
                    BalanceBefore = buyerWallet.AvailableBalance,
                    BalanceAfter = buyerWallet.AvailableBalance - holdAmount,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = orderId,
                    Description = $"Thanh toan tien hang cho don {orderId}",
                    CreatedAt = now
                };
                buyerWallet.AvailableBalance -= holdAmount;

                // Ledger 1.2: Cộng tiền Seller (In) -> Vào Hold
                var sellerLedger = new wallet_ledger
                {
                    LedgerId = Guid.NewGuid(),
                    WalletTransactionId = sellerWalletTx.WalletTransactionId,
                    WalletId = sellerWallet.WalletId,
                    Direction = (int)LedgerDirection.In,
                    BalanceType = (int)BalanceType.Hold,
                    Amount = holdAmount,
                    BalanceBefore = sellerWallet.HoldBalance,
                    BalanceAfter = sellerWallet.HoldBalance + holdAmount,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = orderId,
                    Description = $"Tam giu tien cho don hang {orderId}",
                    CreatedAt = DateTime.UtcNow
                };
                sellerWallet.HoldBalance += holdAmount;


                // HẠCH TOÁN 2: TIỀN VỀ HỆ THỐNG (Phí ship GHN nếu có)
                if (needsSystemLedger && systemWallet != null)
                {
                    systemWalletTx = new wallet_transaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        FromWalletId = buyerWallet.WalletId,
                        ToWalletId = systemWallet.WalletId,
                        PaymentId = paymentId,
                        ReferenceId = orderId,
                        ReferenceType = (int)ReferenceType.Order,
                        TransactionType = (int)TransactionType.Shipping_Fee_Collected,
                        Amount = shippingFee,
                        WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                        CreatedAt = now
                    };

                    // Ledger 2.1: Trừ tiền Buyer phần phí ship (Out)
                    buyerLedgerForSystem = new wallet_ledger
                    {
                        LedgerId = Guid.NewGuid(),
                        WalletTransactionId = systemWalletTx.WalletTransactionId,
                        WalletId = buyerWallet.WalletId,
                        Direction = (int)LedgerDirection.Out,
                        BalanceType = (int)BalanceType.Available,
                        Amount = shippingFee,
                        BalanceBefore = buyerWallet.AvailableBalance,
                        BalanceAfter = buyerWallet.AvailableBalance - shippingFee,
                        ReferenceType = (int)ReferenceType.Order,
                        ReferenceId = orderId,
                        Description = $"Thanh toan phi ship GHN cho don {orderId}",
                        CreatedAt = now
                    };
                    buyerWallet.AvailableBalance -= shippingFee;

                    // Ledger 2.2: Cộng tiền System (In)
                    systemLedger = new wallet_ledger
                    {
                        LedgerId = Guid.NewGuid(),
                        WalletTransactionId = systemWalletTx.WalletTransactionId,
                        WalletId = systemWallet.WalletId,
                        Direction = (int)LedgerDirection.In,
                        BalanceType = (int)BalanceType.Available, // tiền pass-through, không phải hold
                        Amount = shippingFee,
                        BalanceBefore = systemWallet.AvailableBalance,
                        BalanceAfter = systemWallet.AvailableBalance + shippingFee,
                        ReferenceType = (int)ReferenceType.Order,
                        ReferenceId = orderId,
                        Description = $"Phi van chuyen GHN thu ho cho don hang {orderId}",
                        CreatedAt = now
                    };
                    systemWallet.AvailableBalance += shippingFee;
                    systemWallet.UpdatedAt = now;
                }

                // Khởi tạo thực thể Payment (Wallet-specific: Completed ngay lập tức, không qua gateway ngoài)
                var payment = new payment
                {
                    PaymentId = paymentId,
                    AgreementId = agreement.AgreementId,
                    PayerId = payerId,
                    PaymentType = agreement.PaymentType,
                    PaymentMethod = (int)PaymentMethod.Internal_Wallet, 
                    Amount = amountToPay,
                    OrderId = orderId,
                    Description = "Thanh toan qua Vi noi bo",
                    PaymentStatus = (int)PaymentStatus.Completed, // Trạng thái Completed ngay lập tức
                    CreatedAt = now,
                    PaidAt = now
                };

                // Hiện thực hóa Agreement -> Order/Appointment/trừ Quantity/Confirmed (dùng chung với PayOS)
                await FulfillAgreementAsync(agreement, basePrice, amountToPay, details, ct, orderIdOverride: orderId);

                // Lưu Data
                buyerWallet.UpdatedAt = now;
                sellerWallet.UpdatedAt = now;
                if (systemWalletTx != null) await _walletTxRepo.AddAsync(systemWalletTx, ct);
                if (buyerLedgerForSystem != null) await _ledgerRepo.AddAsync(buyerLedgerForSystem, ct);
                if (systemLedger != null) await _ledgerRepo.AddAsync(systemLedger, ct);
                if (needsSystemLedger && systemWallet != null) await _walletRepo.UpdateAsync(systemWallet, ct);
                await _walletRepo.UpdateAsync(buyerWallet, ct);
                await _walletRepo.UpdateAsync(sellerWallet, ct);
                await _paymentRepo.AddAsync(payment, ct);
                await _walletTxRepo.AddAsync(sellerWalletTx, ct);
                await _ledgerRepo.AddAsync(buyerLedgerForSeller, ct);
                await _ledgerRepo.AddAsync(sellerLedger, ct);

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

        public async Task<Result<string>> SyncPaymentStatusAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<string>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != payerId)
                return Result<string>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền xem trạng thái thanh toán này."));

            var pending = await _paymentRepo.GetLatestPendingByAgreementAsync(agreementId, ct);
            if (pending == null)
            {
                var currentStatus = agreement.AgreementStatus == (int)AgreementStatus.Confirmed
                    ? PaymentStatus.Completed
                    : PaymentStatus.Pending;
                return Result<string>.Success(currentStatus.ToString());
            }

            var tx = await _paymentTxRepo.GetLatestByPaymentIdAsync(pending.PaymentId, ct);
            if (tx == null)
                return Result<string>.Fail(new Error("Payment.TransactionNotFound", "Không tìm thấy giao dịch tương ứng."));

            // Nếu đã hết hạn từ trước, không cần gọi PayOS nữa -> trả Expired ngay và đồng bộ cả 2 bảng.
            if (pending.ExpiredAt.HasValue && pending.ExpiredAt.Value <= DateTime.UtcNow
                && pending.PaymentStatus != (int)PaymentStatus.Completed)
            {
                pending.PaymentStatus = (int)PaymentStatus.Expired;
                tx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Failed;
                tx.UpdatedAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(pending, ct);
                await _paymentTxRepo.UpdateAsync(tx, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result<string>.Success(PaymentStatus.Expired.ToString());
            }

            var statusResult = await _gatewayService.GetPaymentStatusAsync(tx.PayOSOrderCode, ct);
            if (!statusResult.IsSuccess)
            {
                _logger.LogWarning("SyncPaymentStatusAsync: gọi PayOS thất bại cho OrderCode {OrderCode}, Agreement {AgreementId}",
                    tx.PayOSOrderCode, agreementId);
                return Result<string>.Fail(statusResult.Error);
            }

            switch (statusResult.Data.Status?.ToUpperInvariant())
            {
                case "PAID":
                    // Webhook có thể bị delay/miss — chủ động fulfill luôn nếu phát hiện đã PAID thật.
                    // ExecuteSuccessfulPaymentCoreAsync đã có guard idempotent (check PaymentTransactionStatus == Success).
                    await ExecuteSuccessfulPaymentCoreAsync(tx.PayOSOrderCode, statusResult.Data.TransactionId ?? string.Empty, ct);
                    return Result<string>.Success(PaymentStatus.Completed.ToString());

                case "CANCELLED":
                    pending.PaymentStatus = (int)PaymentStatus.Cancelled;
                    tx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Cancelled;
                    tx.UpdatedAt = DateTime.UtcNow;
                    await _paymentRepo.UpdateAsync(pending, ct);
                    await _paymentTxRepo.UpdateAsync(tx, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                    return Result<string>.Success(PaymentStatus.Cancelled.ToString());

                case "PENDING":
                case "PROCESSING":
                    if (pending.ExpiredAt.HasValue && pending.ExpiredAt.Value <= DateTime.UtcNow)
                    {
                        pending.PaymentStatus = (int)PaymentStatus.Expired;
                        tx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Failed;
                        tx.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepo.UpdateAsync(pending, ct);
                        await _paymentTxRepo.UpdateAsync(tx, ct);
                        await _unitOfWork.SaveChangesAsync(ct);
                        return Result<string>.Success(PaymentStatus.Expired.ToString());
                    }
                    return Result<string>.Success(PaymentStatus.Pending.ToString());

                default:
                    _logger.LogWarning("SyncPaymentStatusAsync: nhận status lạ '{Status}' từ PayOS cho OrderCode {OrderCode}",
                        statusResult.Data.Status, tx.PayOSOrderCode);
                    return Result<string>.Success(PaymentStatus.Pending.ToString());
            }
        }

        public async Task<Result<PagedResult<PaymentHistoryResponseDto>>> GetMyPaymentHistoryAsync(Guid userId, PaymentHistorySearchRequest request, CancellationToken ct = default)
        {
            var result = await _paymentRepo.GetPagedPaymentHistoryAsync(userId, request, ct);
            return Result<PagedResult<PaymentHistoryResponseDto>>.Success(result);
        }

        //public async Task<Result<bool>> RefundOrderHeldAmountAsync(
        //    order order,
        //    agreement_form agreement,
        //    decimal amount,
        //    CancellationToken ct = default)
        //{
        //    var currentOrderPaid = order.AmountPaid ?? 0;

        //    if (amount <= AmountEpsilon ||
        //        currentOrderPaid <= AmountEpsilon ||
        //        amount > currentOrderPaid + AmountEpsilon)
        //    {
        //        return Result<bool>.Fail(PaymentErrors.InvalidRefundAmount);
        //    }

        //    var payment = await _paymentRepo.GetLatestPaidByOrderIdAsync(
        //        order.OrderId,
        //        ct);

        //    if (payment == null)
        //        return Result<bool>.Fail(PaymentErrors.RefundPaymentNotFound);

        //    if (payment.PaymentStatus == (int)PaymentStatus.Refunded)
        //        return Result<bool>.Fail(PaymentErrors.AlreadyRefunded);

        //    wallet? buyerWallet;
        //    wallet? sellerWallet;

        //    // Giữ deterministic locking giống payment flow hiện tại.
        //    if (string.Compare(
        //            agreement.BuyerId.ToString(),
        //            agreement.SellerId.ToString(),
        //            StringComparison.Ordinal) < 0)
        //    {
        //        buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
        //            agreement.BuyerId,
        //            ct);

        //        sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
        //            agreement.SellerId,
        //            ct);
        //    }
        //    else
        //    {
        //        sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
        //            agreement.SellerId,
        //            ct);

        //        buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
        //            agreement.BuyerId,
        //            ct);
        //    }

        //    if (buyerWallet == null || sellerWallet == null)
        //        return Result<bool>.Fail(PaymentErrors.RefundWalletNotFound);

        //    if (sellerWallet.HoldBalance + AmountEpsilon < amount)
        //        return Result<bool>.Fail(PaymentErrors.InsufficientHeldBalance);

        //    var now = DateTime.UtcNow;
        //    var walletTransactionId = Guid.NewGuid();

        //    var walletTransaction = new wallet_transaction
        //    {
        //        WalletTransactionId = walletTransactionId,

        //        FromWalletId = sellerWallet.WalletId,
        //        ToWalletId = buyerWallet.WalletId,

        //        PaymentId = payment.PaymentId,

        //        ReferenceId = order.OrderId,
        //        ReferenceType = (int)ReferenceType.Order,

        //        TransactionType = (int)TransactionType.Order_Refund,

        //        Amount = amount,

        //        WalletTransactionStatus =
        //            (int)WalletTransactionStatus.Completed,

        //        CreatedAt = now
        //    };

        //    var sellerLedger = new wallet_ledger
        //    {
        //        LedgerId = Guid.NewGuid(),

        //        WalletTransactionId = walletTransactionId,
        //        WalletId = sellerWallet.WalletId,

        //        Direction = (int)LedgerDirection.Out,
        //        BalanceType = (int)BalanceType.Hold,

        //        Amount = amount,

        //        BalanceBefore = sellerWallet.HoldBalance,
        //        BalanceAfter = sellerWallet.HoldBalance - amount,

        //        ReferenceType = (int)ReferenceType.Order,
        //        ReferenceId = order.OrderId,

        //        Description =
        //            $"Hoan tien tam giu cho Order {order.OrderId}",

        //        CreatedAt = now
        //    };

        //    var buyerLedger = new wallet_ledger
        //    {
        //        LedgerId = Guid.NewGuid(),

        //        WalletTransactionId = walletTransactionId,
        //        WalletId = buyerWallet.WalletId,

        //        Direction = (int)LedgerDirection.In,
        //        BalanceType = (int)BalanceType.Available,

        //        Amount = amount,

        //        BalanceBefore = buyerWallet.AvailableBalance,
        //        BalanceAfter = buyerWallet.AvailableBalance + amount,

        //        ReferenceType = (int)ReferenceType.Order,
        //        ReferenceId = order.OrderId,

        //        Description =
        //            $"Nhan hoan tien tu Order {order.OrderId}",

        //        CreatedAt = now
        //    };

        //    sellerWallet.HoldBalance -= amount;
        //    sellerWallet.UpdatedAt = now;

        //    buyerWallet.AvailableBalance += amount;
        //    buyerWallet.UpdatedAt = now;

        //    // QUAN TRỌNG:
        //    // So với AmountPaid hiện tại của Order,
        //    // KHÔNG so với Payment.Amount ban đầu.
        //    var isFullRefund =
        //        amount >= currentOrderPaid - AmountEpsilon;

        //    payment.PaymentStatus = isFullRefund
        //        ? (int)PaymentStatus.Refunded
        //        : (int)PaymentStatus.PartiallyRefunded;

        //    await _walletRepo.UpdateAsync(sellerWallet, ct);
        //    await _walletRepo.UpdateAsync(buyerWallet, ct);

        //    await _walletTxRepo.AddAsync(walletTransaction, ct);

        //    await _ledgerRepo.AddAsync(sellerLedger, ct);
        //    await _ledgerRepo.AddAsync(buyerLedger, ct);

        //    await _paymentRepo.UpdateAsync(payment, ct);

        //    return Result<bool>.Success(true);
        //}
        public async Task<Result<bool>> RefundOrderHeldAmountAsync(
            order order,
            agreement_form agreement,
            decimal amount,
            CancellationToken ct = default)
        {
            var result = await RefundOrderHeldAmountCoreAsync(order, agreement, amount, ct);

            if (!result.IsSuccess)
                return Result<bool>.Fail(result.Error!);

            return Result<bool>.Success(true);
        }

        public Task<Result<decimal>> RefundAllRemainingOrderHeldAmountAsync(
            order order,
            agreement_form agreement,
            CancellationToken ct = default)
        {
            return RefundOrderHeldAmountCoreAsync(order, agreement, null, ct);
        }


        #region HELPER

        private async Task<Result<decimal>> RefundOrderHeldAmountCoreAsync(
            order order,
            agreement_form agreement,
            decimal? requestedAmount,
            CancellationToken ct)
        {
            var currentOrderPaid = order.AmountPaid ?? 0;

            if (currentOrderPaid <= AmountEpsilon)
                return Result<decimal>.Fail(PaymentErrors.InvalidRefundAmount);

            var payment = await _paymentRepo.GetLatestPaidByOrderIdAsync(order.OrderId, ct);

            if (payment == null)
                return Result<decimal>.Fail(PaymentErrors.RefundPaymentNotFound);

            if (payment.PaymentStatus == (int)PaymentStatus.Refunded)
                return Result<decimal>.Fail(PaymentErrors.AlreadyRefunded);

            wallet? buyerWallet;
            wallet? sellerWallet;

            if (string.Compare(
                    agreement.BuyerId.ToString(),
                    agreement.SellerId.ToString(),
                    StringComparison.Ordinal) < 0)
            {
                buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.BuyerId, ct);
                sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.SellerId, ct);
            }
            else
            {
                sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.SellerId, ct);
                buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.BuyerId, ct);
            }

            if (buyerWallet == null || sellerWallet == null)
                return Result<decimal>.Fail(PaymentErrors.RefundWalletNotFound);

            var orderHeldAmount = await _ledgerRepo.GetNetOrderHeldAmountAsync(
                sellerWallet.WalletId,
                order.OrderId,
                ct);

            if (orderHeldAmount <= AmountEpsilon)
                return Result<decimal>.Fail(PaymentErrors.OrderHeldAmountNotFound);

            var amount = requestedAmount ?? orderHeldAmount;

            if (amount <= AmountEpsilon ||
                amount > currentOrderPaid + AmountEpsilon ||
                amount > orderHeldAmount + AmountEpsilon)
            {
                return Result<decimal>.Fail(PaymentErrors.InvalidRefundAmount);
            }

            if (sellerWallet.HoldBalance + AmountEpsilon < amount)
                return Result<decimal>.Fail(PaymentErrors.InsufficientHeldBalance);

            var now = DateTime.UtcNow;
            var walletTransactionId = Guid.NewGuid();

            var walletTransaction = new wallet_transaction
            {
                WalletTransactionId = walletTransactionId,
                FromWalletId = sellerWallet.WalletId,
                ToWalletId = buyerWallet.WalletId,
                PaymentId = payment.PaymentId,
                ReferenceId = order.OrderId,
                ReferenceType = (int)ReferenceType.Order,
                TransactionType = (int)TransactionType.Order_Refund,
                Amount = amount,
                WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                CreatedAt = now
            };

            var sellerLedger = new wallet_ledger
            {
                LedgerId = Guid.NewGuid(),
                WalletTransactionId = walletTransactionId,
                WalletId = sellerWallet.WalletId,
                Direction = (int)LedgerDirection.Out,
                BalanceType = (int)BalanceType.Hold,
                Amount = amount,
                BalanceBefore = sellerWallet.HoldBalance,
                BalanceAfter = sellerWallet.HoldBalance - amount,
                ReferenceType = (int)ReferenceType.Order,
                ReferenceId = order.OrderId,
                Description = $"Hoan tien tam giu cho Order {order.OrderId}",
                CreatedAt = now
            };

            var buyerLedger = new wallet_ledger
            {
                LedgerId = Guid.NewGuid(),
                WalletTransactionId = walletTransactionId,
                WalletId = buyerWallet.WalletId,
                Direction = (int)LedgerDirection.In,
                BalanceType = (int)BalanceType.Available,
                Amount = amount,
                BalanceBefore = buyerWallet.AvailableBalance,
                BalanceAfter = buyerWallet.AvailableBalance + amount,
                ReferenceType = (int)ReferenceType.Order,
                ReferenceId = order.OrderId,
                Description = $"Nhan hoan tien tu Order {order.OrderId}",
                CreatedAt = now
            };

            sellerWallet.HoldBalance -= amount;
            sellerWallet.UpdatedAt = now;

            buyerWallet.AvailableBalance += amount;
            buyerWallet.UpdatedAt = now;

            payment.PaymentStatus = amount >= currentOrderPaid - AmountEpsilon
                ? (int)PaymentStatus.Refunded
                : (int)PaymentStatus.PartiallyRefunded;

            await _walletRepo.UpdateAsync(sellerWallet, ct);
            await _walletRepo.UpdateAsync(buyerWallet, ct);
            await _walletTxRepo.AddAsync(walletTransaction, ct);
            await _ledgerRepo.AddAsync(sellerLedger, ct);
            await _ledgerRepo.AddAsync(buyerLedger, ct);
            await _paymentRepo.UpdateAsync(payment, ct);

            return Result<decimal>.Success(amount);
        }

        private async Task ExecuteSuccessfulPaymentCoreAsync(string payOsOrderCode, string payOsTransactionId, CancellationToken ct)
        {
            var paymentTx = await _paymentTxRepo.GetByPayOSOrderCodeAsync(payOsOrderCode, ct);
            if (paymentTx == null || paymentTx.PaymentTransactionStatus == (int)PaymentTransactionStatus.Success)
                return; // chặn Webhook gọi 2 lần 

            var payment = await _paymentRepo.GetByIdAsync(paymentTx.PaymentId, ct);
            var agreement = await _agreementRepo.GetByIdAsync(payment.AgreementId.Value, ct);

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

            decimal holdAmount = details?.DeliveryMethod == DeliveryMethod.GhnDelivery
                ? basePrice
                : paidAmount;

            decimal shippingFee = details?.DeliveryMethod == DeliveryMethod.GhnDelivery
                ? (details?.EstimatedShippingFee ?? Math.Max(paidAmount - basePrice, 0))
                : 0;
            bool needsSystemLedger = details?.DeliveryMethod == DeliveryMethod.GhnDelivery && shippingFee > 0;
            


            await _unitOfWork.BeginTransactionAsync();
            try
            {
                wallet_transaction? systemWalletTx = null;
                wallet_ledger? systemLedger = null;

                var sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(agreement.SellerId, ct);
                if (sellerWallet == null)
                    throw new InvalidOperationException("Không tìm thấy ví người bán."); // Sẽ bị catch bên dưới và rollback

                wallet? systemWallet = null;
                if (needsSystemLedger)
                {
                    systemWallet = await _walletRepo.GetSystemWalletForUpdateAsync(SystemWalletPurpose.Shipping_Escrow, ct);
                    if (systemWallet == null)
                        throw new InvalidOperationException("Không tìm thấy ví hệ thống để nhận phí vận chuyển.");
                }

                paymentTx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Success;
                paymentTx.PayOSTransactionId = payOsTransactionId;
                paymentTx.UpdatedAt = DateTime.UtcNow;

                payment.PaymentStatus = (int)PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;

                // Hiện thực hóa Agreement -> Order/Appointment/trừ Quantity/Confirmed (dùng chung với Wallet)
                var fulfillment = await FulfillAgreementAsync(agreement, basePrice, paidAmount, details, ct);

                payment.OrderId = fulfillment.Order.OrderId;

                // Hạch toán ví (Escrow Logic) — chỉ ghi nhận CHIỀU VÀO cho seller, vì tiền buyer đã rời hệ thống qua PayOS, không qua ví nội bộ.
                var newWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    FromWalletId = null,
                    ToWalletId = sellerWallet.WalletId,
                    PaymentId = payment.PaymentId,
                    ReferenceId = fulfillment.Order.OrderId,
                    ReferenceType = (int)ReferenceType.Order,
                    TransactionType = (int)TransactionType.Escrow_Deposit,
                    Amount = holdAmount,
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
                    Amount = holdAmount,
                    BalanceBefore = sellerWallet.HoldBalance,
                    BalanceAfter = sellerWallet.HoldBalance + holdAmount,
                    ReferenceType = (int)ReferenceType.Order,
                    ReferenceId = fulfillment.Order.OrderId,
                    Description = $"Tam giu tien cho don hang {fulfillment.Order.OrderId}",
                    CreatedAt = DateTime.UtcNow
                };

                sellerWallet.HoldBalance += holdAmount;
                sellerWallet.UpdatedAt = DateTime.UtcNow;

                // HẠCH TOÁN 2: TIỀN VỀ HỆ THỐNG (Phí ship từ PayOS nếu có)
                if (needsSystemLedger && systemWallet != null)
                {
                    systemWalletTx = new wallet_transaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        FromWalletId = null,
                        ToWalletId = systemWallet.WalletId,
                        PaymentId = payment.PaymentId,
                        ReferenceId = fulfillment.Order.OrderId,
                        ReferenceType = (int)ReferenceType.Order,
                        TransactionType = (int)TransactionType.Shipping_Fee_Collected,
                        Amount = shippingFee,
                        WalletTransactionStatus = (int)WalletTransactionStatus.Completed,
                        CreatedAt = DateTime.UtcNow
                    };

                    systemLedger = new wallet_ledger
                    {
                        LedgerId = Guid.NewGuid(),
                        WalletTransactionId = systemWalletTx.WalletTransactionId,
                        WalletId = systemWallet.WalletId,
                        Direction = (int)LedgerDirection.In,
                        BalanceType = (int)BalanceType.Available,
                        Amount = shippingFee,
                        BalanceBefore = systemWallet.AvailableBalance,
                        BalanceAfter = systemWallet.AvailableBalance + shippingFee,
                        ReferenceType = (int)ReferenceType.Order,
                        ReferenceId = fulfillment.Order.OrderId,
                        Description = $"Phi van chuyen GHN thu qua PayOS cho don hang {fulfillment.Order.OrderId}",
                        CreatedAt = DateTime.UtcNow
                    };

                    systemWallet.AvailableBalance += shippingFee;
                    systemWallet.UpdatedAt = DateTime.UtcNow;
                }

                await _paymentTxRepo.UpdateAsync(paymentTx, ct);
                await _paymentRepo.UpdateAsync(payment, ct);
                await _walletTxRepo.AddAsync(newWalletTx, ct);
                await _ledgerRepo.AddAsync(newLedger, ct);
                await _walletRepo.UpdateAsync(sellerWallet, ct);

                if (systemWalletTx != null)
                    await _walletTxRepo.AddAsync(systemWalletTx, ct);

                if (systemLedger != null)
                    await _ledgerRepo.AddAsync(systemLedger, ct);

                if (needsSystemLedger && systemWallet != null)
                    await _walletRepo.UpdateAsync(systemWallet, ct);

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
            //decimal shippingFee = details?.EstimatedShippingFee ?? 0;
            decimal configuredShippingFee = details?.EstimatedShippingFee ?? 0;
            decimal shippingFee = 0;

            decimal amountToPay;
            int paymentType;

            if (agreement.AgreementType == (int)AgreementType.Inspection)
            {
                amountToPay = basePrice * DEPOSIT_RATE;
                paymentType = (int)PaymentType.Deposit;
            }
            else if (deliveryMethod == DeliveryMethod.GhnDelivery)
            {
                // GHN -> thanh toán toàn bộ tiền hàng + phí giao hàng đã chốt (đã tính qua API GHN)
                shippingFee = configuredShippingFee;
                amountToPay = basePrice + shippingFee;
                paymentType = (int)PaymentType.Full_Payment;
            }
            else
            {
                // BuyerPickUp / SellerDelivers / fallback: giữ nguyên loại thanh toán đã chốt lúc tạo Agreement.
                paymentType = agreement.PaymentType ?? (int)PaymentType.Full_Payment;
                decimal itemPay = paymentType == (int)PaymentType.Deposit ? basePrice * DEPOSIT_RATE : basePrice;
                //amountToPay = deliveryMethod == DeliveryMethod.SellerDelivers ? itemPay + shippingFee : itemPay;
                if (deliveryMethod == DeliveryMethod.SellerDelivers)
                    shippingFee = configuredShippingFee;

                amountToPay = itemPay + shippingFee;
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

        private static void ValidateGhnParcel(int weightGram, int lengthCm, int widthCm, int heightCm, string label)
        {
            if (weightGram < 1 || weightGram > GhnMaxWeightGram)
                throw new InvalidOperationException($"Khối lượng {label} phải từ 1 đến {GhnMaxWeightGram} gram.");

            if (lengthCm < GhnMinDimensionCm || lengthCm > GhnMaxDimensionCm ||
                widthCm < GhnMinDimensionCm || widthCm > GhnMaxDimensionCm ||
                heightCm < GhnMinDimensionCm || heightCm > GhnMaxDimensionCm)
                throw new InvalidOperationException($"Kích thước {label} phải từ 1 đến {GhnMaxDimensionCm} cm.");
        }

        private sealed class FulfillmentResult
        {
            public order Order { get; init; } = null!;
            public appointment Appointment { get; init; } = null!;
            public inspection_appointment? InspectionAppointment { get; init; }
            public collection_appointment? CollectionAppointment { get; init; }

            public shipment? Shipment { get; init; } //mới thêm
            public ghn_shipment? GhnShipment { get; init; } //mới thêm

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

            // Chống fulfill trùng: 1 Agreement chỉ được phát sinh đúng 1 Order.
            // (Lưu ý: để chặn tuyệt đối khi webhook gọi song song, cần thêm unique index trên Order.AgreementId.)
            var existingOrder = await _orderRepo.GetByAgreementIdAsync(agreement.AgreementId, ct);
            if (existingOrder != null)
                throw new InvalidOperationException("Thỏa thuận đã phát sinh đơn hàng, không thể tạo lại.");

            // Tổng đơn phải bao gồm đúng khoản phí vận chuyển đã được thu.
            // Inspection chưa thu phí giao hàng; BuyerPickUp không có phí giao hàng.
            decimal shippingFee = agreement.AgreementType != (int)AgreementType.Inspection
                && details?.DeliveryMethod is DeliveryMethod.GhnDelivery or DeliveryMethod.SellerDelivers
                    ? details?.EstimatedShippingFee ?? 0
                    : 0;
            decimal finalTotalAmount = basePrice + shippingFee;

            // Không dựa vào invariant ngầm "Deposit chỉ tồn tại với Inspection":
            // xác định trạng thái thanh toán trực tiếp từ số tiền thực tế.
            decimal amountRemaining = finalTotalAmount - paidAmount;

            if (paidAmount <= 0 || amountRemaining < -AmountEpsilon)
                throw new InvalidOperationException("Số tiền thanh toán không hợp lệ.");

            bool isFullyPaid = amountRemaining <= AmountEpsilon;

            var paymentStatus = isFullyPaid
                ? PaymentStatus.Completed
                : PaymentStatus.Pending;

            var postSnapshot = await _postRepo.GetByIdAsync(agreement.PostId, ct);
            if (postSnapshot == null)
                throw new InvalidOperationException("Không tìm thấy bài đăng của thỏa thuận.");


            var order = new order
            {
                OrderId = orderId,
                OrderCode = GenerateOrderCode(),
                AgreementId = agreement.AgreementId,
                PostId = agreement.PostId,
                ProductName = postSnapshot.Product.ProductName,
                Quantity = agreement.Quantity,
                OriginalTotalAmount = basePrice,
                FinalTotalAmount = finalTotalAmount,
                AmountPaid = paidAmount,
                AmountRemaining = Math.Max(amountRemaining, 0),
                PaymentStatus = (int)paymentStatus,
                OrderStatus = (int)OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };


            var appointmentType = agreement.AgreementType == (int)AgreementType.Inspection
                ? AppointmentType.Inspection
                : AppointmentType.Collection;

            DateTime? scheduledAt = appointmentType == AppointmentType.Inspection
                ? details?.InspectionDate
                : details?.CollectionDate;

            if (!scheduledAt.HasValue)
            {
                throw new InvalidOperationException(
                    "Agreement không có thời gian lịch hẹn hợp lệ.");
            }

            var appointmentPolicy =
                await _platformPolicyProvider
                    .GetAppointmentConfigAsync(ct);

            var supportsLateThreshold =
                appointmentType == AppointmentType.Inspection ||
                details?.DeliveryMethod == DeliveryMethod.BuyerPickUp ||
                details?.DeliveryMethod == DeliveryMethod.SellerDelivers;

            var appointmentId = Guid.NewGuid();
            var appointment = new appointment
            {
                AppointmentId = appointmentId,
                AgreementId = agreement.AgreementId,
                AppointmentType = (int)appointmentType,
                AppointmentStatus = (int)AppointmentStatus.Scheduled,
                LateThresholdAt = supportsLateThreshold
                    ? scheduledAt.Value.AddMinutes(appointmentPolicy.LateThresholdMinutes)
                    : null,
                CreatedAt = DateTime.UtcNow
                // UpdatedAt: để null.
            };

            inspection_appointment? inspectionAppt = null;
            collection_appointment? collectionAppt = null;

            shipment? localShipment = null;
            ghn_shipment? localGhnShipment = null;

            if (appointmentType == AppointmentType.Inspection)
            {
                inspectionAppt = new inspection_appointment
                {
                    InspectionAppointmentId = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    InspectionAddress = details?.InspectionAddress ?? string.Empty,
                    InspectionDate = scheduledAt.Value
                };
                await _inspectionRepo.AddAsync(inspectionAppt, ct);
            }
            else
            {
                collectionAppt = new collection_appointment
                {
                    CollectionAppointmentId = Guid.NewGuid(),
                    AppointmentId = appointmentId,
                    CollectionDate = scheduledAt.Value,
                    PickupAddress = details?.PickupAddress,
                    DeliveryAddress = details?.DeliveryAddress,
                    DeliveryMethod = details?.DeliveryMethod?.ToString()
                };
                await _collectionRepo.AddAsync(collectionAppt, ct);
            }

            var now = DateTime.UtcNow;

            // Chỉ tạo vận đơn GHN khi đã thanh toán đủ (không phải cọc).
            bool shouldCreateGhnShipment =
                isFullyPaid
                && agreement.AgreementType != (int)AgreementType.Inspection
                && details?.DeliveryMethod == DeliveryMethod.GhnDelivery;

            var ghnInfo = shouldCreateGhnShipment
                ? details?.GhnInfo
                : null;

            if (shouldCreateGhnShipment)
            {
                if (ghnInfo == null)
                    throw new InvalidOperationException("Agreement chọn GHN nhưng thiếu GhnInfo.");

                if (ghnInfo.Sender == null)
                    throw new InvalidOperationException("Agreement thiếu snapshot người gửi GHN.");

                if (ghnInfo.Receiver == null)
                    throw new InvalidOperationException("Agreement thiếu snapshot người nhận GHN.");

                if (ghnInfo.ServiceTypeId is not (2 or 5))
                    throw new InvalidOperationException("ServiceTypeId GHN chỉ nhận 2 hoặc 5.");

                if (ghnInfo.Sender.Address is null ||
                    ghnInfo.Sender.Address.DistrictId <= 0 ||
                    string.IsNullOrWhiteSpace(ghnInfo.Sender.Address.WardCode))
                    throw new InvalidOperationException("Agreement thiếu địa chỉ người gửi GHN (DistrictId/WardCode).");

                if (ghnInfo.Receiver.Address is null ||
                    ghnInfo.Receiver.Address.DistrictId <= 0 ||
                    string.IsNullOrWhiteSpace(ghnInfo.Receiver.Address.WardCode))
                    throw new InvalidOperationException("Agreement thiếu địa chỉ người nhận GHN (DistrictId/WardCode).");

                // Thông tin liên hệ bắt buộc cho Create Order.
                if (string.IsNullOrWhiteSpace(ghnInfo.Sender.FullName))
                    throw new InvalidOperationException("Agreement thiếu tên người gửi GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.Sender.Phone))
                    throw new InvalidOperationException("Agreement thiếu số điện thoại người gửi GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.Sender.Address.AddressDetail))
                    throw new InvalidOperationException("Agreement thiếu địa chỉ chi tiết người gửi GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.Receiver.FullName))
                    throw new InvalidOperationException("Agreement thiếu tên người nhận GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.Receiver.Phone))
                    throw new InvalidOperationException("Agreement thiếu số điện thoại người nhận GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.Receiver.Address.AddressDetail))
                    throw new InvalidOperationException("Agreement thiếu địa chỉ chi tiết người nhận GHN.");

                if (string.IsNullOrWhiteSpace(ghnInfo.RequiredNote) ||
                    !ValidGhnRequiredNotes.Contains(ghnInfo.RequiredNote.Trim()))
                    throw new InvalidOperationException("RequiredNote GHN không hợp lệ.");

                // Hàng nhẹ (2) dùng LightParcel; hàng nặng (5) bắt buộc có Items.
                if (ghnInfo.ServiceTypeId == 2)
                {
                    var parcel = ghnInfo.LightParcel;
                    if (parcel is null)
                        throw new InvalidOperationException("Agreement thiếu thông tin kiện hàng nhẹ GHN (LightParcel).");

                    ValidateGhnParcel(
                        parcel.WeightGram, parcel.LengthCm, parcel.WidthCm, parcel.HeightCm,
                        "kiện hàng nhẹ");
                }

                if (ghnInfo.ServiceTypeId == 5)
                {
                    if (ghnInfo.Items == null || ghnInfo.Items.Count == 0)
                        throw new InvalidOperationException("Agreement chưa có thông tin kiện hàng GHN.");

                    long totalWeight = 0;
                    foreach (var item in ghnInfo.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.Name))
                            throw new InvalidOperationException("Kiện hàng GHN thiếu tên sản phẩm.");

                        if (item.Quantity <= 0)
                            throw new InvalidOperationException($"Kiện hàng '{item.Name}' phải có số lượng > 0.");

                        ValidateGhnParcel(
                            item.WeightGram, item.LengthCm, item.WidthCm, item.HeightCm,
                            $"kiện hàng '{item.Name}'");

                        totalWeight += (long)item.WeightGram * item.Quantity;
                    }

                    if (totalWeight is < 1 or > GhnMaxWeightGram)
                        throw new InvalidOperationException("Tổng khối lượng hàng nặng GHN không hợp lệ.");
                }

                if (details?.EstimatedShippingFee is null or < 0)
                    throw new InvalidOperationException("Agreement chưa có phí GHN hợp lệ.");
            }

            // KHỞI TẠO LUỒNG VẬN ĐƠN (SHIPMENT) — chỉ khi thanh toán đủ:
            // - GhnDelivery                : Shipment + GHN_Shipment (CreationStatus = Pending, chưa gửi GHN)
            // - SellerDelivers/BuyerPickUp : chỉ Shipment
            // - Inspection (chỉ đóng cọc)  : chưa tạo shipment
            bool shouldCreateShipment =
                isFullyPaid
                && agreement.AgreementType != (int)AgreementType.Inspection
                && details?.DeliveryMethod is
                    DeliveryMethod.GhnDelivery or DeliveryMethod.SellerDelivers or DeliveryMethod.BuyerPickUp;

            if (shouldCreateShipment)
            {
                var deliveryMethod = details!.DeliveryMethod!.Value;
                var shipmentId = Guid.NewGuid();

                var sender = ghnInfo?.Sender;
                var receiver = ghnInfo?.Receiver;

                localShipment = new shipment
                {
                    ShipmentId = shipmentId,
                    OrderId = orderId,
                    CollectionAppointmentId = collectionAppt?.CollectionAppointmentId,
                    DeliveryMethod = deliveryMethod,
                    ShipmentStatus = ShipmentStatus.ReadyToPick,
                    FromName = sender?.FullName,
                    FromPhone = sender?.Phone,
                    PickupAddress = sender?.Address?.AddressDetail ?? details?.PickupAddress,
                    ToName = receiver?.FullName,
                    ToPhone = receiver?.Phone,
                    DeliveryAddress = receiver?.Address?.AddressDetail ?? details?.DeliveryAddress,
                    SellerReadyAt = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _shipmentRepo.AddAsync(localShipment, ct);

                if (deliveryMethod == DeliveryMethod.GhnDelivery)
                {
                    var senderSnapshot = sender!;
                    var receiverSnapshot = receiver!;

                    int? weight = null;
                    int? length = null;
                    int? width = null;
                    int? height = null;

                    if (ghnInfo!.ServiceTypeId == 2 && ghnInfo.LightParcel is not null)
                    {
                        weight = ghnInfo.LightParcel.WeightGram;
                        length = ghnInfo.LightParcel.LengthCm;
                        width = ghnInfo.LightParcel.WidthCm;
                        height = ghnInfo.LightParcel.HeightCm;
                    }
                    else if (ghnInfo.ServiceTypeId == 5 && ghnInfo.Items.Count > 0)
                    {
                        
                        weight = ghnInfo.Items.Sum(x => x.WeightGram * x.Quantity);

                        var largestItem = ghnInfo.Items
                            .OrderByDescending(x => (long)x.WeightGram * x.Quantity)
                            .First();
                        length = largestItem.LengthCm;
                        width = largestItem.WidthCm;
                        height = largestItem.HeightCm;
                    }

                    localGhnShipment = new ghn_shipment
                    {
                        GHNShipmentId = Guid.NewGuid(),
                        ShipmentId = shipmentId,

                        // Mã được tạo một lần và giữ nguyên trong mọi lần retry.
                        ClientOrderCode = $"HC-{shipmentId:N}",
                        GHNOrderCode = null,

                        ServiceTypeId = ghnInfo.ServiceTypeId!.Value,
                        FromDistrictId = senderSnapshot.Address.DistrictId,
                        FromWardCode = senderSnapshot.Address.WardCode,
                        ToDistrictId = receiverSnapshot.Address.DistrictId,
                        ToWardCode = receiverSnapshot.Address.WardCode,

                        Weight = weight,
                        Length = length,
                        Width = width,
                        Height = height,

                        // Buyer đã trả phí ship cho HomeCycle (qua PayOS/ví nội bộ),
                        // GHN thu phí từ ShopId -> payment_type_id = 1, không thu COD.
                        CODAmount = 0,
                        PaymentTypeId = 1,
                        InsuranceValue = 0,
                        RequiredNote = ghnInfo.RequiredNote,

                        // Phí thực tế chỉ có khi Create Order thành công.
                        GHNServiceFee = null,
                        GHNCodFee = null,
                        GHNTotalFee = null,

                        ExpectedDeliveryAt = null,
                        CreationStatus = GHNCreationStatus.Pending,
                        LastCreateAttemptAt = null,
                        LastSyncedAt = null,
                        LastErrorCode = null,
                        CreatedAt = now
                    };

                    await _ghnShipmentRepo.AddAsync(localGhnShipment, ct);
                }
            }

            await _appointmentRepo.AddAsync(appointment, ct);
            await _orderRepo.AddAsync(order, ct);

            // Trừ số lượng còn lại của Post — dùng FOR UPDATE để serialize giữa các giao dịch
            // đồng thời (chống oversell: còn 5 mà 2 giao dịch cùng trừ 4 đều thành công).
            var postForUpdate = await _postRepo.GetByIdForUpdateAsync(agreement.PostId, ct);
            if (postForUpdate == null)
                throw new InvalidOperationException("Không tìm thấy bài đăng của thỏa thuận.");

            if (postForUpdate.RemainingQuantity < agreement.Quantity)
                throw new InvalidOperationException($"Bài đăng chỉ còn {postForUpdate.RemainingQuantity} sản phẩm, không đủ cho {agreement.Quantity}.");

            postForUpdate.RemainingQuantity -= agreement.Quantity;
            if (postForUpdate.RemainingQuantity <= 0)
                postForUpdate.Status = PostStatus.Closed;

            await _postRepo.UpdateAsync(postForUpdate, ct);

            agreement.AgreementStatus = (int)AgreementStatus.Confirmed;
            await _agreementRepo.UpdateAsync(agreement, ct);

            return new FulfillmentResult
            {
                Order = order,
                Appointment = appointment,
                InspectionAppointment = inspectionAppt,
                CollectionAppointment = collectionAppt,

                Shipment = localShipment,
                GhnShipment = localGhnShipment,

                Post = postForUpdate
            };
        }

        private static string GenerateOrderCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyMMdd");
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            return $"HC-{datePart}-{randomPart}";
        }

        #endregion
    }
}