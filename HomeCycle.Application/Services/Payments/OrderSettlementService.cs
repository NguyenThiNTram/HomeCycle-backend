using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Payments
{
    using FluentValidation;
    using global::HomeCycle.Application.Commons.Errors;
    using global::HomeCycle.Application.Commons.Results;
    using global::HomeCycle.Application.DTOs.Requests.Payments;
    using global::HomeCycle.Application.DTOs.Responses.Notifications;
    using global::HomeCycle.Application.DTOs.Responses.Payments;
    using global::HomeCycle.Application.Interfaces.Externals;
    using global::HomeCycle.Application.Interfaces.Generics;
    using global::HomeCycle.Application.Interfaces.Repositories.Agreements;
    using global::HomeCycle.Application.Interfaces.Repositories.Appointments;
    using global::HomeCycle.Application.Interfaces.Repositories.Banks;
    using global::HomeCycle.Application.Interfaces.Repositories.Orders;
    using global::HomeCycle.Application.Interfaces.Repositories.Payments;
    using global::HomeCycle.Application.Interfaces.Repositories.Shipments;
    using global::HomeCycle.Application.Interfaces.Repositories.Wallets;
    using global::HomeCycle.Application.Interfaces.Services.Notifications;
    using global::HomeCycle.Application.Interfaces.Services.Payments;
    using global::HomeCycle.Domain.Entities;
    using global::HomeCycle.Domain.Enums;
    using Microsoft.Extensions.Logging;

    namespace HomeCycle.Application.Services.Payments
    {
        public sealed class OrderSettlementService : IOrderSettlementService
        {
            private const decimal AmountEpsilon = 0.01m;
            private static readonly TimeSpan PaymentTtl = TimeSpan.FromMinutes(15);

            private readonly IUnitOfWork _unitOfWork;
            private readonly IPaymentGatewayService _gatewayService;
            private readonly IPaymentRepository _paymentRepo;
            private readonly IPaymentTransactionRepository _paymentTransactionRepo;
            private readonly IOrderRepository _orderRepo;
            private readonly IAgreementFormRepository _agreementRepo;
            private readonly IShipmentRepository _shipmentRepo;
            private readonly ICollectionAppointmentRepository _collectionRepo;
            private readonly IWalletRepository _walletRepo;
            private readonly IWalletTransactionRepository _walletTransactionRepo;
            private readonly IWalletLedgerRepository _walletLedgerRepo;
            private readonly IBankAccountRepository _bankAccountRepo;
            private readonly INotificationService _notificationService;
            private readonly IValidator<PayOSCheckoutRequest> _payOsCheckoutValidator;
            private readonly ILogger<OrderSettlementService> _logger;

            public OrderSettlementService(
                IUnitOfWork unitOfWork,
                IPaymentGatewayService gatewayService,
                IPaymentRepository paymentRepo,
                IPaymentTransactionRepository paymentTransactionRepo,
                IOrderRepository orderRepo,
                IAgreementFormRepository agreementRepo,
                IShipmentRepository shipmentRepo,
                ICollectionAppointmentRepository collectionRepo,
                IWalletRepository walletRepo,
                IWalletTransactionRepository walletTransactionRepo,
                IWalletLedgerRepository walletLedgerRepo,
                IBankAccountRepository bankAccountRepo,
                INotificationService notificationService,
                IValidator<PayOSCheckoutRequest> payOsCheckoutValidator,
                ILogger<OrderSettlementService> logger)
            {
                _unitOfWork = unitOfWork;
                _gatewayService = gatewayService;
                _paymentRepo = paymentRepo;
                _paymentTransactionRepo = paymentTransactionRepo;
                _orderRepo = orderRepo;
                _agreementRepo = agreementRepo;
                _shipmentRepo = shipmentRepo;
                _collectionRepo = collectionRepo;
                _walletRepo = walletRepo;
                _walletTransactionRepo = walletTransactionRepo;
                _walletLedgerRepo = walletLedgerRepo;
                _bankAccountRepo = bankAccountRepo;
                _notificationService = notificationService;
                _payOsCheckoutValidator = payOsCheckoutValidator;
                _logger = logger;
            }

            public async Task<Result<string>> GeneratePayOsCheckoutUrlAsync(
                Guid paymentId,
                Guid payerId,
                string returnUrl,
                string cancelUrl,
                CancellationToken cancellationToken = default)
            {
                var validation = await _payOsCheckoutValidator.ValidateAsync(
                    new PayOSCheckoutRequest
                    {
                        ReturnUrl = returnUrl,
                        CancelUrl = cancelUrl
                    },
                    cancellationToken);

                if (!validation.IsValid)
                {
                    return Result<string>.Fail(
                        new Error(
                            "Payment.InvalidRedirectUrl",
                            string.Join(" ", validation.Errors.Select(x => x.ErrorMessage))));
                }

                var contextResult = await GetSettlementContextAsync(
                    paymentId,
                    payerId,
                    cancellationToken);

                if (!contextResult.IsSuccess)
                    return Result<string>.Fail(contextResult.Error!);

                var context = contextResult.Data!;
                var payment = context.Payment;
                var order = context.Order;

                if (!await HasVerifiedBankAccountAsync(payerId, cancellationToken))
                    return Result<string>.Fail(PaymentErrors.BankAccountNotVerified);

                var currentStatus = GetPaymentStatus(payment);

                if (currentStatus == PaymentStatus.Completed)
                    return Result<string>.Fail(PaymentErrors.OrderSettlementInvalidStatus);

                if (!IsSettlementAmountValid(payment, order))
                    return Result<string>.Fail(PaymentErrors.OrderSettlementAmountMismatch);

                var existingTransaction =
                    await _paymentTransactionRepo.GetLatestByPaymentIdAsync(
                        paymentId,
                        cancellationToken);

                if (currentStatus == PaymentStatus.Pending
                    && existingTransaction?.PaymentTransactionStatus ==
                        (int)PaymentTransactionStatus.Pending
                    && payment.ExpiredAt.HasValue
                    && payment.ExpiredAt.Value > DateTime.UtcNow
                    && !string.IsNullOrWhiteSpace(existingTransaction.CheckoutUrl))
                {
                    return Result<string>.Success(existingTransaction.CheckoutUrl);
                }

                var payOsOrderCodeResult = await GeneratePayOsOrderCodeAsync(
                    cancellationToken);

                if (!payOsOrderCodeResult.IsSuccess)
                    return Result<string>.Fail(payOsOrderCodeResult.Error!);

                var amount = payment.Amount ?? 0;

                if (amount <= 0 || amount > int.MaxValue || amount != decimal.Truncate(amount))
                {
                    return Result<string>.Fail(
                        new Error(
                            "Payment.InvalidAmount",
                            "Số tiền thanh toán không hợp lệ hoặc vượt giới hạn PayOS."));
                }

                var gatewayResult = await _gatewayService.CreatePaymentLinkAsync(
                    new GatewayPaymentRequest
                    {
                        OrderCode = payOsOrderCodeResult.Data,
                        Amount = checked((int)amount),
                        Description = $"TT ORDER {order.OrderCode[^Math.Min(order.OrderCode.Length, 8)..]}",
                        BuyerName = "Buyer",
                        BuyerEmail = "buyer@homecycle.vn",
                        ReturnUrl = returnUrl,
                        CancelUrl = cancelUrl
                    },
                    cancellationToken);

                if (!gatewayResult.IsSuccess)
                    return Result<string>.Fail(gatewayResult.Error!);

                if (gatewayResult.Data == null
                    || string.IsNullOrWhiteSpace(gatewayResult.Data.CheckoutUrl))
                {
                    return Result<string>.Fail(
                        new Error(
                            "Payment.InvalidGatewayResponse",
                            "PayOS không trả về đường dẫn thanh toán hợp lệ."));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var lockedPayment = await _paymentRepo.GetByIdForUpdateAsync(
                        paymentId,
                        cancellationToken);

                    if (lockedPayment == null || !lockedPayment.OrderId.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<string>.Fail(PaymentErrors.OrderSettlementNotFound);
                    }

                    if (GetPaymentStatus(lockedPayment) == PaymentStatus.Completed)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<string>.Fail(PaymentErrors.OrderSettlementInvalidStatus);
                    }

                    var now = DateTime.UtcNow;

                    lockedPayment.PaymentMethod = (int)PaymentMethod.PayOS;
                    lockedPayment.PaymentStatus = (int)PaymentStatus.Pending;
                    lockedPayment.ExpiredAt = now.Add(PaymentTtl);

                    var paymentTransaction = new payment_transaction
                    {
                        PaymentTransactionId = Guid.NewGuid(),
                        PaymentId = lockedPayment.PaymentId,
                        UserId = payerId,
                        PayOSOrderCode = payOsOrderCodeResult.Data.ToString(),
                        PayOSPaymentLinkId = gatewayResult.Data.PaymentLinkId,
                        CheckoutUrl = gatewayResult.Data.CheckoutUrl,
                        PaymentTransactionStatus = (int)PaymentTransactionStatus.Pending,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _paymentRepo.UpdateAsync(
                        lockedPayment,
                        cancellationToken);

                    await _paymentTransactionRepo.AddAsync(
                        paymentTransaction,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return Result<string>.Success(
                        gatewayResult.Data.CheckoutUrl);
                }
                catch (Exception exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    _logger.LogError(
                        exception,
                        "Không thể tạo PayOS checkout cho OrderSettlement Payment {PaymentId}.",
                        paymentId);

                    return Result<string>.Fail(
                        new Error(
                            "Payment.CreateFailed",
                            "Không thể khởi tạo thanh toán PayOS."));
                }
            }

            public async Task<Result<PaymentStatusResponseDto>> ExecuteWalletAsync(
                Guid paymentId,
                Guid payerId,
                CancellationToken cancellationToken = default)
            {
                if (!await HasVerifiedBankAccountAsync(payerId, cancellationToken))
                {
                    return Result<PaymentStatusResponseDto>.Fail(
                        PaymentErrors.BankAccountNotVerified);
                }

                notification? sellerNotification = null;
                notification? buyerNotification = null;

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var payment = await _paymentRepo.GetByIdForUpdateAsync(
                        paymentId,
                        cancellationToken);

                    if (payment == null || !payment.OrderId.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(
                            PaymentErrors.OrderSettlementNotFound);
                    }

                    var order = await _orderRepo.GetByIdForUpdateAsync(
                        payment.OrderId.Value,
                        cancellationToken);

                    if (order == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(OrderErrors.NotFound);
                    }

                    var agreement = await _agreementRepo.GetByIdAsync(
                        order.AgreementId,
                        cancellationToken);

                    if (agreement == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(AgreementErrors.NotFound);
                    }

                    if (payment.PayerId != payerId || agreement.BuyerId != payerId)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(
                            new Error("Auth.Forbidden", "Bạn không có quyền thanh toán đơn hàng này."));
                    }

                    var currentStatus = GetPaymentStatus(payment);

                    if (currentStatus == PaymentStatus.Completed)
                    {
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        return Result<PaymentStatusResponseDto>.Success(
                            await BuildStatusResponseAsync(
                                payment,
                                PaymentStatus.Completed,
                                cancellationToken));
                    }

                    if (currentStatus != PaymentStatus.Pending)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(
                            PaymentErrors.OrderSettlementInvalidStatus);
                    }

                    if (payment.PaymentMethod == (int)PaymentMethod.PayOS)
                    {
                        var pendingPayOsTransaction =
                            await _paymentTransactionRepo.GetLatestByPaymentIdAsync(
                                payment.PaymentId,
                                cancellationToken);

                        if (pendingPayOsTransaction?.PaymentTransactionStatus ==
                            (int)PaymentTransactionStatus.Pending)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                            return Result<PaymentStatusResponseDto>.Fail(
                                new Error(
                                    "Payment.PayOsCheckoutActive",
                                    "Khoản thanh toán đang có liên kết PayOS còn hiệu lực."));
                        }
                    }

                    if (!IsSettlementAmountValid(payment, order))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<PaymentStatusResponseDto>.Fail(
                            PaymentErrors.OrderSettlementAmountMismatch);
                    }

                    var settlementResult = await ApplySettlementAsync(
                        payment,
                        order,
                        agreement,
                        PaymentMethod.Internal_Wallet,
                        cancellationToken);

                    if (!settlementResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                        return Result<PaymentStatusResponseDto>.Fail(
                            settlementResult.Error!);
                    }

                    sellerNotification = await AddSettlementNotificationAsync(
                        agreement.SellerId,
                        "Đã nhận thanh toán bổ sung",
                        "Người mua đã thanh toán phần còn lại của đơn hàng. Bạn có thể tiếp tục chuẩn bị hàng.",
                        order.OrderId,
                        cancellationToken);

                    buyerNotification = await AddSettlementNotificationAsync(
                        payerId,
                        "Thanh toán thành công",
                        "Phần tiền còn lại của đơn hàng đã được thanh toán thành công.",
                        order.OrderId,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    await _notificationService.PublishCreatedSafelyAsync(
                        sellerNotification);

                    await _notificationService.PublishCreatedSafelyAsync(
                        buyerNotification);

                    return Result<PaymentStatusResponseDto>.Success(
                        await BuildStatusResponseAsync(
                            payment,
                            PaymentStatus.Completed,
                            cancellationToken));
                }
                catch (Exception exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    _logger.LogError(
                        exception,
                        "Không thể thanh toán OrderSettlement bằng ví cho Payment {PaymentId}.",
                        paymentId);

                    return Result<PaymentStatusResponseDto>.Fail(
                        new Error(
                            "Payment.WalletSettlementFailed",
                            "Không thể hoàn tất thanh toán bằng ví."));
                }
            }

            public async Task<Result<PaymentStatusResponseDto>> SyncPayOsStatusAsync(
                Guid paymentId,
                Guid payerId,
                CancellationToken cancellationToken = default)
            {
                var contextResult = await GetSettlementContextAsync(
                    paymentId,
                    payerId,
                    cancellationToken);

                if (!contextResult.IsSuccess)
                {
                    return Result<PaymentStatusResponseDto>.Fail(
                        contextResult.Error!);
                }

                var context = contextResult.Data!;
                var payment = context.Payment;
                var currentStatus = GetPaymentStatus(payment);

                if (currentStatus == PaymentStatus.Completed)
                {
                    return Result<PaymentStatusResponseDto>.Success(
                        await BuildStatusResponseAsync(
                            payment,
                            PaymentStatus.Completed,
                            cancellationToken));
                }

                var transaction =
                    await _paymentTransactionRepo.GetLatestByPaymentIdAsync(
                        payment.PaymentId,
                        cancellationToken);

                if (transaction == null
                    || string.IsNullOrWhiteSpace(transaction.PayOSOrderCode))
                {
                    return Result<PaymentStatusResponseDto>.Fail(
                        new Error(
                            "Payment.TransactionNotFound",
                            "Không tìm thấy giao dịch PayOS tương ứng."));
                }

                if (payment.ExpiredAt.HasValue
                    && payment.ExpiredAt.Value <= DateTime.UtcNow)
                {
                    var terminalStatus = await ApplyTerminalStatusAsync(
                        payment.PaymentId,
                        transaction.PayOSOrderCode,
                        PaymentStatus.Expired,
                        PaymentTransactionStatus.Failed,
                        cancellationToken);

                    return Result<PaymentStatusResponseDto>.Success(
                        await BuildStatusResponseAsync(
                            payment,
                            terminalStatus,
                            cancellationToken));
                }

                var statusResult = await _gatewayService.GetPaymentStatusAsync(
                    transaction.PayOSOrderCode,
                    cancellationToken);

                if (!statusResult.IsSuccess)
                    return Result<PaymentStatusResponseDto>.Fail(statusResult.Error!);

                switch (statusResult.Data.Status?.Trim().ToUpperInvariant())
                {
                    case "PAID":
                        {
                            var completeResult = await CompletePayOsAsync(
                                transaction.PayOSOrderCode,
                                statusResult.Data.TransactionId ?? string.Empty,
                                cancellationToken);

                            if (!completeResult.IsSuccess)
                            {
                                return Result<PaymentStatusResponseDto>.Fail(
                                    completeResult.Error!);
                            }

                            return Result<PaymentStatusResponseDto>.Success(
                                await BuildStatusResponseAsync(
                                    payment,
                                    PaymentStatus.Completed,
                                    cancellationToken));
                        }

                    case "CANCELLED":
                        {
                            var terminalStatus = await ApplyTerminalStatusAsync(
                                payment.PaymentId,
                                transaction.PayOSOrderCode,
                                PaymentStatus.Cancelled,
                                PaymentTransactionStatus.Cancelled,
                                cancellationToken);

                            return Result<PaymentStatusResponseDto>.Success(
                                await BuildStatusResponseAsync(
                                    payment,
                                    terminalStatus,
                                    cancellationToken));
                        }

                    case "PENDING":
                    case "PROCESSING":
                        return Result<PaymentStatusResponseDto>.Success(
                            await BuildStatusResponseAsync(
                                payment,
                                PaymentStatus.Pending,
                                cancellationToken));

                    default:
                        _logger.LogWarning(
                            "PayOS trả status chưa hỗ trợ {Status} cho settlement Payment {PaymentId}.",
                            statusResult.Data.Status,
                            payment.PaymentId);

                        return Result<PaymentStatusResponseDto>.Success(
                            await BuildStatusResponseAsync(
                                payment,
                                PaymentStatus.Pending,
                                cancellationToken));
                }
            }

            public async Task<Result> CompletePayOsAsync(
                string payOsOrderCode,
                string payOsTransactionId,
                CancellationToken cancellationToken = default)
            {
                notification? sellerNotification = null;
                notification? buyerNotification = null;

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var transaction =
                        await _paymentTransactionRepo.GetByPayOSOrderCodeForUpdateAsync(
                            payOsOrderCode,
                            cancellationToken);

                    if (transaction == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                        return Result.Fail(
                            new Error(
                                "Payment.TransactionNotFound",
                                "Không tìm thấy giao dịch PayOS tương ứng."));
                    }

                    if (transaction.PaymentTransactionStatus ==
                        (int)PaymentTransactionStatus.Success)
                    {
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);
                        return Result.Success();
                    }

                    var payment = await _paymentRepo.GetByIdForUpdateAsync(
                        transaction.PaymentId,
                        cancellationToken);

                    if (payment == null || !payment.OrderId.HasValue)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(PaymentErrors.OrderSettlementNotFound);
                    }

                    var order = await _orderRepo.GetByIdForUpdateAsync(
                        payment.OrderId.Value,
                        cancellationToken);

                    if (order == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(OrderErrors.NotFound);
                    }

                    var agreement = await _agreementRepo.GetByIdAsync(
                        order.AgreementId,
                        cancellationToken);

                    if (agreement == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(AgreementErrors.NotFound);
                    }

                    if (GetPaymentStatus(payment) != PaymentStatus.Pending)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(PaymentErrors.OrderSettlementInvalidStatus);
                    }

                    if (!IsSettlementAmountValid(payment, order))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(PaymentErrors.OrderSettlementAmountMismatch);
                    }

                    var settlementResult = await ApplySettlementAsync(
                        payment,
                        order,
                        agreement,
                        PaymentMethod.PayOS,
                        cancellationToken);

                    if (!settlementResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Fail(settlementResult.Error!);
                    }

                    transaction.PaymentTransactionStatus =
                        (int)PaymentTransactionStatus.Success;

                    transaction.PayOSTransactionId =
                        string.IsNullOrWhiteSpace(payOsTransactionId)
                            ? transaction.PayOSTransactionId
                            : payOsTransactionId.Trim();

                    transaction.UpdatedAt = DateTime.UtcNow;

                    await _paymentTransactionRepo.UpdateAsync(
                        transaction,
                        cancellationToken);

                    sellerNotification = await AddSettlementNotificationAsync(
                        agreement.SellerId,
                        "Đã nhận thanh toán bổ sung",
                        "Người mua đã thanh toán phần còn lại của đơn hàng. Bạn có thể tiếp tục chuẩn bị hàng.",
                        order.OrderId,
                        cancellationToken);

                    buyerNotification = await AddSettlementNotificationAsync(
                        payment.PayerId,
                        "Thanh toán thành công",
                        "Phần tiền còn lại của đơn hàng đã được thanh toán thành công.",
                        order.OrderId,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    await _notificationService.PublishCreatedSafelyAsync(
                        sellerNotification);

                    await _notificationService.PublishCreatedSafelyAsync(
                        buyerNotification);

                    return Result.Success();
                }
                catch (Exception exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    _logger.LogError(
                        exception,
                        "Không thể hoàn tất PayOS OrderSettlement {PayOsOrderCode}.",
                        payOsOrderCode);

                    return Result.Fail(
                        new Error(
                            "Payment.SettlementFailed",
                            "Không thể hạch toán khoản thanh toán bổ sung."));
                }
            }

            private async Task<Result> ApplySettlementAsync(
                payment payment,
                order order,
                agreement_form agreement,
                PaymentMethod paymentMethod,
                CancellationToken cancellationToken)
            {
                var amount = payment.Amount ?? 0;

                if (amount <= 0)
                    return Result.Fail(PaymentErrors.OrderSettlementAmountMismatch);

                var shipment = await _shipmentRepo.GetByOrderIdAsync(
                    order.OrderId,
                    cancellationToken);

                if (shipment == null)
                    return Result.Fail(ShipmentErrors.NotFound);

                collection_appointment? collectionAppointment = null;

                if (shipment.CollectionAppointmentId.HasValue)
                {
                    collectionAppointment = await _collectionRepo.GetByIdAsync(
                        shipment.CollectionAppointmentId.Value,
                        cancellationToken);
                }

                var shippingFee =
                    shipment.DeliveryMethod == DeliveryMethod.GhnDelivery
                        ? collectionAppointment?.EstimatedShippingFee ?? 0
                        : 0;

                if (shippingFee < 0 || shippingFee - amount > AmountEpsilon)
                {
                    return Result.Fail(
                        new Error(
                            "Payment.InvalidShippingFee",
                            "Phí GHN không hợp lệ so với khoản thanh toán."));
                }

                var sellerAmount = shipment.DeliveryMethod ==
                    DeliveryMethod.GhnDelivery
                        ? amount - shippingFee
                        : amount;

                wallet? buyerWallet = null;
                wallet? sellerWallet;

                if (paymentMethod == PaymentMethod.Internal_Wallet)
                {
                    if (string.Compare(
                            payment.PayerId.ToString(),
                            agreement.SellerId.ToString(),
                            StringComparison.Ordinal) < 0)
                    {
                        buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
                            payment.PayerId,
                            cancellationToken);

                        sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
                            agreement.SellerId,
                            cancellationToken);
                    }
                    else
                    {
                        sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
                            agreement.SellerId,
                            cancellationToken);

                        buyerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
                            payment.PayerId,
                            cancellationToken);
                    }

                    if (buyerWallet == null)
                    {
                        return Result.Fail(
                            new Error(
                                "Wallet.BuyerNotFound",
                                "Không tìm thấy ví người mua."));
                    }

                    if (buyerWallet.AvailableBalance < amount)
                    {
                        return Result.Fail(
                            new Error(
                                "Wallet.InsufficientBalance",
                                "Số dư ví không đủ để thanh toán."));
                    }
                }
                else
                {
                    sellerWallet = await _walletRepo.GetUserWalletForUpdateAsync(
                        agreement.SellerId,
                        cancellationToken);
                }

                if (sellerWallet == null)
                {
                    return Result.Fail(
                        new Error(
                            "Wallet.SellerNotFound",
                            "Không tìm thấy ví người bán."));
                }

                wallet? systemWallet = null;

                if (shippingFee > AmountEpsilon)
                {
                    systemWallet = await _walletRepo.GetSystemWalletForUpdateAsync(
                        SystemWalletPurpose.Shipping_Escrow,
                        cancellationToken);

                    if (systemWallet == null)
                    {
                        return Result.Fail(
                            new Error(
                                "Wallet.SystemWalletNotFound",
                                "Không tìm thấy ví hệ thống nhận phí GHN."));
                    }
                }

                var now = DateTime.UtcNow;

                if (sellerAmount > AmountEpsilon)
                {
                    var sellerTransaction = new wallet_transaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        FromWalletId = buyerWallet?.WalletId,
                        ToWalletId = sellerWallet.WalletId,
                        PaymentId = payment.PaymentId,
                        ReferenceId = order.OrderId,
                        ReferenceType = (int)ReferenceType.Order,
                        TransactionType = paymentMethod == PaymentMethod.PayOS
                            ? (int)TransactionType.Escrow_Deposit
                            : (int)TransactionType.Wallet_Payment,
                        Amount = sellerAmount,
                        WalletTransactionStatus =
                            (int)WalletTransactionStatus.Completed,
                        CreatedAt = now
                    };

                    if (buyerWallet != null)
                    {
                        var buyerBalanceBefore = buyerWallet.AvailableBalance;

                        buyerWallet.AvailableBalance -= sellerAmount;

                        await _walletLedgerRepo.AddAsync(
                            new wallet_ledger
                            {
                                LedgerId = Guid.NewGuid(),
                                WalletTransactionId =
                                    sellerTransaction.WalletTransactionId,
                                WalletId = buyerWallet.WalletId,
                                Direction = (int)LedgerDirection.Out,
                                BalanceType = (int)BalanceType.Available,
                                Amount = sellerAmount,
                                BalanceBefore = buyerBalanceBefore,
                                BalanceAfter = buyerWallet.AvailableBalance,
                                ReferenceType = (int)ReferenceType.Order,
                                ReferenceId = order.OrderId,
                                Description =
                                    $"Thanh toán phần tiền hàng còn lại của đơn {order.OrderCode}",
                                CreatedAt = now
                            },
                            cancellationToken);
                    }

                    var sellerBalanceBefore = sellerWallet.HoldBalance;

                    sellerWallet.HoldBalance += sellerAmount;

                    await _walletTransactionRepo.AddAsync(
                        sellerTransaction,
                        cancellationToken);

                    await _walletLedgerRepo.AddAsync(
                        new wallet_ledger
                        {
                            LedgerId = Guid.NewGuid(),
                            WalletTransactionId =
                                sellerTransaction.WalletTransactionId,
                            WalletId = sellerWallet.WalletId,
                            Direction = (int)LedgerDirection.In,
                            BalanceType = (int)BalanceType.Hold,
                            Amount = sellerAmount,
                            BalanceBefore = sellerBalanceBefore,
                            BalanceAfter = sellerWallet.HoldBalance,
                            ReferenceType = (int)ReferenceType.Order,
                            ReferenceId = order.OrderId,
                            Description =
                                $"Tạm giữ phần tiền hàng còn lại của đơn {order.OrderCode}",
                            CreatedAt = now
                        },
                        cancellationToken);
                }

                if (shippingFee > AmountEpsilon && systemWallet != null)
                {
                    var shippingTransaction = new wallet_transaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        FromWalletId = buyerWallet?.WalletId,
                        ToWalletId = systemWallet.WalletId,
                        PaymentId = payment.PaymentId,
                        ReferenceId = order.OrderId,
                        ReferenceType = (int)ReferenceType.Order,
                        TransactionType =
                            (int)TransactionType.Shipping_Fee_Collected,
                        Amount = shippingFee,
                        WalletTransactionStatus =
                            (int)WalletTransactionStatus.Completed,
                        CreatedAt = now
                    };

                    if (buyerWallet != null)
                    {
                        var buyerBalanceBefore = buyerWallet.AvailableBalance;

                        buyerWallet.AvailableBalance -= shippingFee;

                        await _walletLedgerRepo.AddAsync(
                            new wallet_ledger
                            {
                                LedgerId = Guid.NewGuid(),
                                WalletTransactionId =
                                    shippingTransaction.WalletTransactionId,
                                WalletId = buyerWallet.WalletId,
                                Direction = (int)LedgerDirection.Out,
                                BalanceType = (int)BalanceType.Available,
                                Amount = shippingFee,
                                BalanceBefore = buyerBalanceBefore,
                                BalanceAfter = buyerWallet.AvailableBalance,
                                ReferenceType = (int)ReferenceType.Order,
                                ReferenceId = order.OrderId,
                                Description =
                                    $"Thanh toán phí GHN của đơn {order.OrderCode}",
                                CreatedAt = now
                            },
                            cancellationToken);
                    }

                    var systemBalanceBefore = systemWallet.AvailableBalance;

                    systemWallet.AvailableBalance += shippingFee;

                    await _walletTransactionRepo.AddAsync(
                        shippingTransaction,
                        cancellationToken);

                    await _walletLedgerRepo.AddAsync(
                        new wallet_ledger
                        {
                            LedgerId = Guid.NewGuid(),
                            WalletTransactionId =
                                shippingTransaction.WalletTransactionId,
                            WalletId = systemWallet.WalletId,
                            Direction = (int)LedgerDirection.In,
                            BalanceType = (int)BalanceType.Available,
                            Amount = shippingFee,
                            BalanceBefore = systemBalanceBefore,
                            BalanceAfter = systemWallet.AvailableBalance,
                            ReferenceType = (int)ReferenceType.Order,
                            ReferenceId = order.OrderId,
                            Description =
                                $"Thu phí GHN của đơn {order.OrderCode}",
                            CreatedAt = now
                        },
                        cancellationToken);
                }

                if (buyerWallet != null)
                {
                    buyerWallet.UpdatedAt = now;

                    await _walletRepo.UpdateAsync(
                        buyerWallet,
                        cancellationToken);
                }

                sellerWallet.UpdatedAt = now;

                await _walletRepo.UpdateAsync(
                    sellerWallet,
                    cancellationToken);

                if (systemWallet != null)
                {
                    systemWallet.UpdatedAt = now;

                    await _walletRepo.UpdateAsync(
                        systemWallet,
                        cancellationToken);
                }

                payment.PaymentMethod = (int)paymentMethod;
                payment.PaymentStatus = (int)PaymentStatus.Completed;
                payment.PaidAt = now;
                payment.ExpiredAt = null;

                var newAmountPaid = (order.AmountPaid ?? 0) + amount;
                var finalTotalAmount = order.FinalTotalAmount ?? 0;
                var amountRemaining = Math.Max(finalTotalAmount - newAmountPaid, 0);

                order.AmountPaid = newAmountPaid;
                order.AmountRemaining = amountRemaining;
                order.PaymentStatus = amountRemaining <= AmountEpsilon
                    ? (int)PaymentStatus.Completed
                    : (int)PaymentStatus.Pending;
                order.UpdatedAt = now;

                await _paymentRepo.UpdateAsync(
                    payment,
                    cancellationToken);

                await _orderRepo.UpdateAsync(
                    order,
                    cancellationToken);

                return Result.Success();
            }

            private async Task<PaymentStatus> ApplyTerminalStatusAsync(
                Guid paymentId,
                string payOsOrderCode,
                PaymentStatus paymentStatus,
                PaymentTransactionStatus transactionStatus,
                CancellationToken cancellationToken)
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var transaction =
                        await _paymentTransactionRepo.GetByPayOSOrderCodeForUpdateAsync(
                            payOsOrderCode,
                            cancellationToken);

                    if (transaction == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                        throw new InvalidOperationException(
                            "Không tìm thấy giao dịch PayOS.");
                    }

                    if (transaction.PaymentTransactionStatus ==
                        (int)PaymentTransactionStatus.Success)
                    {
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);
                        return PaymentStatus.Completed;
                    }

                    var payment = await _paymentRepo.GetByIdForUpdateAsync(
                        paymentId,
                        cancellationToken);

                    if (payment == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                        throw new InvalidOperationException(
                            "Không tìm thấy payment.");
                    }

                    var currentStatus = GetPaymentStatus(payment);

                    if (currentStatus != PaymentStatus.Pending)
                    {
                        await _unitOfWork.CommitTransactionAsync(cancellationToken);
                        return currentStatus;
                    }

                    payment.PaymentStatus = (int)paymentStatus;
                    transaction.PaymentTransactionStatus = (int)transactionStatus;
                    transaction.UpdatedAt = DateTime.UtcNow;

                    await _paymentRepo.UpdateAsync(
                        payment,
                        cancellationToken);

                    await _paymentTransactionRepo.UpdateAsync(
                        transaction,
                        cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return paymentStatus;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }

            private async Task<Result<OrderSettlementContext>> GetSettlementContextAsync(
                Guid paymentId,
                Guid payerId,
                CancellationToken cancellationToken)
            {
                var payment = await _paymentRepo.GetByIdAsync(
                    paymentId,
                    cancellationToken);

                if (payment == null || !payment.OrderId.HasValue)
                {
                    return Result<OrderSettlementContext>.Fail(
                        PaymentErrors.OrderSettlementNotFound);
                }

                var order = await _orderRepo.GetByIdAsync(
                    payment.OrderId.Value,
                    cancellationToken);

                if (order == null)
                    return Result<OrderSettlementContext>.Fail(OrderErrors.NotFound);

                var agreement = await _agreementRepo.GetByIdAsync(
                    order.AgreementId,
                    cancellationToken);

                if (agreement == null)
                {
                    return Result<OrderSettlementContext>.Fail(
                        AgreementErrors.NotFound);
                }

                if (payment.PayerId != payerId || agreement.BuyerId != payerId)
                {
                    return Result<OrderSettlementContext>.Fail(
                        new Error(
                            "Auth.Forbidden",
                            "Bạn không có quyền thanh toán đơn hàng này."));
                }

                return Result<OrderSettlementContext>.Success(
                    new OrderSettlementContext
                    {
                        Payment = payment,
                        Order = order,
                        Agreement = agreement
                    });
            }

            private async Task<Result<long>> GeneratePayOsOrderCodeAsync(
                CancellationToken cancellationToken)
            {
                const int maxAttempts = 5;

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var orderCode =
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        % 900_000_000
                        + 100_000_000;

                    if (!await _paymentTransactionRepo
                            .ExistsByPayOSOrderCodeAsync(
                                orderCode.ToString(),
                                cancellationToken))
                    {
                        return Result<long>.Success(orderCode);
                    }

                    if (attempt < maxAttempts - 1)
                        await Task.Delay(5, cancellationToken);
                }

                return Result<long>.Fail(
                    new Error(
                        "Payment.OrderCodeConflict",
                        "Không thể tạo mã thanh toán PayOS, vui lòng thử lại."));
            }

            private async Task<PaymentStatusResponseDto> BuildStatusResponseAsync(
                payment payment,
                PaymentStatus status,
                CancellationToken cancellationToken)
            {
                Guid? appointmentId = null;

                if (payment.OrderId.HasValue)
                {
                    var shipment = await _shipmentRepo.GetByOrderIdAsync(
                        payment.OrderId.Value,
                        cancellationToken);

                    if (shipment?.CollectionAppointmentId.HasValue == true)
                    {
                        var collectionAppointment =
                            await _collectionRepo.GetByIdAsync(
                                shipment.CollectionAppointmentId.Value,
                                cancellationToken);

                        appointmentId = collectionAppointment?.AppointmentId;
                    }
                }

                return new PaymentStatusResponseDto
                {
                    PaymentStatus = status,
                    OrderId = payment.OrderId,
                    AppointmentId = appointmentId
                };
            }

            private async Task<bool> HasVerifiedBankAccountAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                var bankAccount = await _bankAccountRepo.GetByUserIdAsync(
                    userId,
                    cancellationToken);

                return bankAccount?.VerifyStatus == VerifyStatus.Verified;
            }

            private Task<notification> AddSettlementNotificationAsync(
                Guid recipientId,
                string title,
                string message,
                Guid orderId,
                CancellationToken cancellationToken)
            {
                return _notificationService.AddPendingAsync(
                    new CreateNotificationCommand(
                        recipientId,
                        title,
                        message,
                        NotificationTargetType.Order,
                        orderId),
                    cancellationToken);
            }

            private static bool IsSettlementAmountValid(
                payment payment,
                order order)
            {
                var paymentAmount = payment.Amount ?? 0;
                var amountRemaining = order.AmountRemaining
                    ?? Math.Max(
                        (order.FinalTotalAmount ?? 0)
                        - (order.AmountPaid ?? 0),
                        0);

                return paymentAmount > 0
                    && Math.Abs(paymentAmount - amountRemaining)
                        <= AmountEpsilon;
            }

            private static PaymentStatus GetPaymentStatus(payment payment)
            {
                return payment.PaymentStatus.HasValue
                    ? (PaymentStatus)payment.PaymentStatus.Value
                    : PaymentStatus.Pending;
            }

            private sealed class OrderSettlementContext
            {
                public required payment Payment { get; init; }
                public required order Order { get; init; }
                public required agreement_form Agreement { get; init; }
            }
        }
    }
}
