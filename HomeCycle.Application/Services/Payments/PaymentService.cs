using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.Interfaces.Externals;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
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
            _logger = logger;
        }

        public async Task<Result<string>> GeneratePayOSCheckoutUrlAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<string>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != payerId)
                return Result<string>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền thanh toán thỏa thuận này."));

            decimal basePrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
            decimal amountToPay = 0;

            DeliveryMethod? deliveryMethod = null;
            decimal shippingFee = 0;

            if (!string.IsNullOrEmpty(agreement.AgreementDetailsJsonb))
            {
                try
                {
                    var detailsOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var details = JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb, detailsOptions);
                    if (details != null)
                    {
                        deliveryMethod = details.DeliveryMethod;
                        shippingFee = details.EstimatedShippingFee ?? 0;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Lỗi parse AgreementDetailsJsonb (PaymentService) cho Agreement {AgreementId}", agreementId);
                    return Result<string>.Fail(new Error("Data.InvalidFormat", "Dữ liệu JSONB cấu hình thỏa thuận bị lỗi."));
                }
            }

            // TÍNH TOÁN DÒNG TIỀN THEO NGHIỆP VỤ
            if (agreement.AgreementType == (int)AgreementType.Inspection)
            {
                amountToPay = basePrice * DEPOSIT_RATE;
                agreement.PaymentType = (int)PaymentType.Deposit;
            }
            else
            {
                if (deliveryMethod == DeliveryMethod.GhnDelivery)
                {
                    amountToPay = basePrice + shippingFee;
                    agreement.PaymentType = (int)PaymentType.Full_Payment;
                }
                else if (deliveryMethod == DeliveryMethod.BuyerPickUp)
                {
                    amountToPay = agreement.PaymentType == (int)PaymentType.Deposit
                        ? (basePrice * DEPOSIT_RATE)
                        : basePrice;
                }
                else if (deliveryMethod == DeliveryMethod.SellerDelivers)
                {
                    decimal itemPay = agreement.PaymentType == (int)PaymentType.Deposit
                        ? (basePrice * DEPOSIT_RATE)
                        : basePrice;
                    amountToPay = itemPay + shippingFee;
                }
                else
                {
                    // Fallback
                    amountToPay = agreement.PaymentType == (int)PaymentType.Deposit
                        ? (basePrice * DEPOSIT_RATE)
                        : basePrice;
                }
            }

            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 900000000 + 100000000;

            var gatewayRequest = new GatewayPaymentRequest
            {
                OrderCode = orderCode,
                Amount = (int)amountToPay,
                Description = $"TT AGREE {agreementId.ToString().Substring(0, 6)}",
                BuyerName = "Buyer",
                BuyerEmail = "buyer@homecycle.vn"
            };

            var gatewayResult = await _gatewayService.CreatePaymentLinkAsync(gatewayRequest, ct);
            if (!gatewayResult.IsSuccess)
                return Result<string>.Fail(gatewayResult.Error);

            var paymentId = Guid.NewGuid();
            var payment = new payment
            {
                PaymentId = paymentId,
                AgreementId = agreement.AgreementId,
                PayerId = payerId,
                PaymentType = agreement.PaymentType,
                PaymentMethod = (int)PaymentMethod.PayOS,
                Amount = amountToPay,
                Description = "Thanh toan qua PayOS",
                PaymentStatus = (int)PaymentStatus.Pending
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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

        private async Task ExecuteSuccessfulPaymentCoreAsync(string payOsOrderCode, string payOsTransactionId, CancellationToken ct)
        {
            var paymentTx = await _paymentTxRepo.GetByPayOSOrderCodeAsync(payOsOrderCode, ct);
            if (paymentTx == null || paymentTx.PaymentTransactionStatus == (int)PaymentTransactionStatus.Success)
                return; // Ngăn chặn Webhook gọi 2 lần (Idempotency)

            var payment = await _paymentRepo.GetByIdAsync(paymentTx.PaymentId, ct);
            var agreement = await _agreementRepo.GetByIdAsync(payment.AgreementId.Value, ct);
            var sellerWallet = await _walletRepo.GetByUserIdAndTypeAsync(agreement.SellerId, WalletTypeEnum.Personal, ct);

            // BÓC TÁCH JSONB
            DateTime? inspectionDate = null;
            string? inspectionAddress = null;

            DateTime? collectionDate = null;
            string? pickupAddress = null;
            string? deliveryAddress = null;
            string? deliveryMethodString = null;

            if (!string.IsNullOrEmpty(agreement.AgreementDetailsJsonb))
            {
                try
                {
                    var detailsOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var details = JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb, detailsOptions);

                    if (details != null)
                    {
                        inspectionDate = details.InspectionDate;
                        inspectionAddress = details.InspectionAddress;

                        collectionDate = details.CollectionDate;
                        pickupAddress = details.PickupAddress;
                        deliveryAddress = details.DeliveryAddress;
                        deliveryMethodString = details.DeliveryMethod?.ToString();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Lỗi bóc tách Jsonb ở hàm ExecuteSuccessfulPaymentCoreAsync cho Agreement {AgreementId}", agreement.AgreementId);
                }
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Cập nhật trạng thái thanh toán
                paymentTx.PaymentTransactionStatus = (int)PaymentTransactionStatus.Success;
                paymentTx.PayOSTransactionId = payOsTransactionId;
                paymentTx.UpdatedAt = DateTime.UtcNow;

                payment.PaymentStatus = (int)PaymentStatus.Completed;
                payment.PaidAt = DateTime.UtcNow;

                decimal basePrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
                decimal paidAmount = payment.Amount ?? 0;

                // 2. Tạo Order
                var orderId = Guid.NewGuid();
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 3. Tạo Appointment (Lịch trình)
                var appointmentId = Guid.NewGuid();
                var appointment = new appointment
                {
                    AppointmentId = appointmentId,
                    AgreementId = agreement.AgreementId,
                    AppointmentType = agreement.AgreementType,
                    AppointmentStatus = 0, // Pending
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _appointmentRepo.AddAsync(appointment, ct);

                if (agreement.AgreementType == (int)AgreementType.Inspection)
                {
                    var inspectionAppt = new inspection_appointment
                    {
                        InspectionAppointmentId = Guid.NewGuid(),
                        AppointmentId = appointmentId,
                        InspectionAddress = inspectionAddress ?? string.Empty,
                        InspectionDate = inspectionDate ?? DateTime.UtcNow.AddDays(1)
                    };
                    await _inspectionRepo.AddAsync(inspectionAppt, ct);
                }
                else
                {
                    var collectionAppt = new collection_appointment
                    {
                        CollectionAppointmentId = Guid.NewGuid(),
                        AppointmentId = appointmentId,
                        CollectionDate = collectionDate,
                        PickupAddress = pickupAddress,
                        DeliveryAddress = deliveryAddress,
                        DeliveryMethod = deliveryMethodString
                    };
                    await _collectionRepo.AddAsync(collectionAppt, ct);
                }

                // 4. Hạch toán ví (Escrow Logic)
                var newWalletTx = new wallet_transaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    ToWalletId = sellerWallet.WalletId,
                    PaymentId = payment.PaymentId,
                    ReferenceId = orderId,
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
                    ReferenceId = orderId,
                    Description = $"Tam giu tien cho don hang {orderId}",
                    CreatedAt = DateTime.UtcNow
                };

                sellerWallet.HoldBalance += paidAmount;
                sellerWallet.UpdatedAt = DateTime.UtcNow;

                // 5. Lưu vào Database
                await _paymentTxRepo.UpdateAsync(paymentTx, ct);
                await _paymentRepo.UpdateAsync(payment, ct);
                await _orderRepo.AddAsync(order, ct);
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

        public async Task<Result<bool>> ExecuteWalletPaymentAsync(Guid agreementId, Guid payerId, CancellationToken ct = default)
        {
            // 1. LẤY VÀ KIỂM TRA DỮ LIỆU CƠ BẢN
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, ct);
            if (agreement == null)
                return Result<bool>.Fail(new Error("Agreement.NotFound", "Không tìm thấy thỏa thuận."));

            if (agreement.BuyerId != payerId)
                return Result<bool>.Fail(new Error("Auth.Forbidden", "Chỉ người mua mới có quyền thanh toán."));

            if (agreement.PaymentType == (int)PaymentType.Deposit || agreement.PaymentType == (int)PaymentType.Full_Payment)
                return Result<bool>.Fail(new Error("Agreement.AlreadyPaid", "Thỏa thuận này đã được xử lý thanh toán."));

            // 2. BÓC TÁCH JSONB VÀ TÍNH TOÁN DÒNG TIỀN (Tương tự luồng PayOS)
            decimal basePrice = agreement.FinalPrice ?? agreement.InitialPrice ?? 0;
            decimal amountToPay = 0;

            DeliveryMethod? deliveryMethod = null;
            decimal shippingFee = 0;
            DateTime? inspectionDate = null, collectionDate = null;
            string? inspectionAddress = null, pickupAddress = null, deliveryAddress = null, deliveryMethodString = null;

            if (!string.IsNullOrEmpty(agreement.AgreementDetailsJsonb))
            {
                try
                {
                    var detailsOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var details = JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb, detailsOptions);
                    if (details != null)
                    {
                        deliveryMethod = details.DeliveryMethod;
                        shippingFee = details.EstimatedShippingFee ?? 0;
                        inspectionDate = details.InspectionDate;
                        inspectionAddress = details.InspectionAddress;
                        collectionDate = details.CollectionDate;
                        pickupAddress = details.PickupAddress;
                        deliveryAddress = details.DeliveryAddress;
                        deliveryMethodString = details.DeliveryMethod?.ToString();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Lỗi parse JSONB (WalletPayment) cho Agreement {AgreementId}", agreementId);
                    return Result<bool>.Fail(new Error("Data.InvalidFormat", "Dữ liệu JSONB bị lỗi."));
                }
            }

            // Logic tính tiền
            if (agreement.AgreementType == (int)AgreementType.Inspection)
            {
                amountToPay = basePrice * DEPOSIT_RATE;
                agreement.PaymentType = (int)PaymentType.Deposit;
            }
            else
            {
                if (deliveryMethod == DeliveryMethod.GhnDelivery)
                {
                    amountToPay = basePrice + shippingFee;
                    agreement.PaymentType = (int)PaymentType.Full_Payment;
                }
                else if (deliveryMethod == DeliveryMethod.BuyerPickUp || deliveryMethod == DeliveryMethod.SellerDelivers)
                {
                    decimal itemPay = agreement.PaymentType == (int)PaymentType.Deposit ? (basePrice * DEPOSIT_RATE) : basePrice;
                    amountToPay = deliveryMethod == DeliveryMethod.SellerDelivers ? itemPay + shippingFee : itemPay;
                }
                else
                {
                    amountToPay = agreement.PaymentType == (int)PaymentType.Deposit ? (basePrice * DEPOSIT_RATE) : basePrice;
                }
            }

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

                // 4.3 Khởi tạo thực thể Payment & Order
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
                    PaidAt = DateTime.UtcNow
                };

                var order = new order
                {
                    OrderId = orderId,
                    AgreementId = agreement.AgreementId,
                    PostId = agreement.PostId,
                    Quantity = agreement.Quantity,
                    OriginalTotalAmount = basePrice,
                    FinalTotalAmount = basePrice,
                    AmountPaid = amountToPay,
                    AmountRemaining = basePrice - amountToPay > 0 ? basePrice - amountToPay : 0,
                    PaymentStatus = agreement.PaymentType == (int)PaymentType.Deposit ? (int)PaymentStatus.Pending : (int)PaymentStatus.Completed,
                    OrderStatus = (int)OrderStatus.Processing,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 4.4 Khởi tạo Appointment
                var appointmentId = Guid.NewGuid();
                var appointment = new appointment
                {
                    AppointmentId = appointmentId,
                    AgreementId = agreement.AgreementId,
                    AppointmentType = agreement.AgreementType,
                    AppointmentStatus = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _appointmentRepo.AddAsync(appointment, ct);

                if (agreement.AgreementType == (int)AgreementType.Inspection)
                {
                    var inspectionAppt = new inspection_appointment
                    {
                        InspectionAppointmentId = Guid.NewGuid(),
                        AppointmentId = appointmentId,
                        InspectionAddress = inspectionAddress ?? string.Empty,
                        InspectionDate = inspectionDate ?? DateTime.UtcNow.AddDays(1)
                    };
                    await _inspectionRepo.AddAsync(inspectionAppt, ct);
                }
                else
                {
                    var collectionAppt = new collection_appointment
                    {
                        CollectionAppointmentId = Guid.NewGuid(),
                        AppointmentId = appointmentId,
                        CollectionDate = collectionDate,
                        PickupAddress = pickupAddress,
                        DeliveryAddress = deliveryAddress,
                        DeliveryMethod = deliveryMethodString
                    };
                    await _collectionRepo.AddAsync(collectionAppt, ct);
                }

                // 4.5 Lưu Data
                await _agreementRepo.UpdateAsync(agreement, ct);
                await _walletRepo.UpdateAsync(buyerWallet, ct);
                await _walletRepo.UpdateAsync(sellerWallet, ct);
                await _walletTxRepo.AddAsync(buyerWalletTx, ct);
                await _ledgerRepo.AddAsync(buyerLedger, ct);
                await _walletTxRepo.AddAsync(sellerWalletTx, ct);
                await _ledgerRepo.AddAsync(sellerLedger, ct);
                await _paymentRepo.AddAsync(payment, ct);
                await _orderRepo.AddAsync(order, ct);

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
    }
}