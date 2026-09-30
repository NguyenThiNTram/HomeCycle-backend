using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Repositories.Payments;
using HomeCycle.Application.Interfaces.Services.Agreements;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Cryptography;
using System.Text.Json;

namespace HomeCycle.Application.Services.Agreements
{
    public sealed class AgreementPdfService : IAgreementPdfService
    {
        private const string PdfFolder = "agreements";
        private readonly IAgreementFormRepository _agreementRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPaymentTransactionRepository _paymentTransactionRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IDisputeRepository _disputeRepo;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditService _auditService;
        private readonly ILogger<AgreementPdfService> _logger;

        static AgreementPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public AgreementPdfService(
            IAgreementFormRepository agreementRepo,
            IPaymentRepository paymentRepo,
            IPaymentTransactionRepository paymentTransactionRepo,
            IOrderRepository orderRepo,
            IDisputeRepository disputeRepo,
            IFileStorageService fileStorageService,
            IAuditService auditService,
            ILogger<AgreementPdfService> logger)
        {
            _agreementRepo = agreementRepo;
            _paymentRepo = paymentRepo;
            _paymentTransactionRepo = paymentTransactionRepo;
            _orderRepo = orderRepo;
            _disputeRepo = disputeRepo;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>Lưu một bản PDF đã chốt lên Firebase và ghi audit hash/đường dẫn để đối chứng.</summary>
        public async Task<Result<bool>> ArchivePaidAgreementPdfAsync(Guid agreementId, Guid paymentId, Guid orderId, CancellationToken cancellationToken = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            var payment = await _paymentRepo.GetByIdAsync(paymentId, cancellationToken);
            var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken);
            if (agreement == null || payment == null || order == null)
                return Result<bool>.Fail(new Error("Agreement.PdfSourceNotFound", "Không tìm thấy dữ liệu hợp đồng, thanh toán hoặc đơn hàng để lưu PDF."));

            if (payment.AgreementId != agreementId || payment.OrderId != orderId || order.AgreementId != agreementId)
                return Result<bool>.Fail(new Error("Agreement.PdfSourceMismatch", "Thông tin hợp đồng, thanh toán và đơn hàng không khớp."));

            if (!IsPaidPayment(payment))
                return Result<bool>.Fail(new Error("Agreement.PdfPaymentNotCompleted", "Chỉ lưu PDF cho hợp đồng đã thanh toán thành công."));

            var path = GetStoragePath(agreementId);
            try
            {
                var existingFile = await _fileStorageService.DownloadFileAsync(path, cancellationToken);
                if (existingFile is { Length: > 0 })
                    return Result<bool>.Success(true);

                var transaction = await _paymentTransactionRepo.GetLatestByPaymentIdAsync(paymentId, cancellationToken);
                var pdfBytes = GeneratePdf(agreement, payment, transaction, order);
                var sha256 = Convert.ToHexString(SHA256.HashData(pdfBytes));
                await using var content = new MemoryStream(pdfBytes, writable: false);
                await _fileStorageService.UploadFileAsync(content, "paid-contract.pdf", $"{PdfFolder}/{agreementId}", overwrite: true);

                await RecordArchiveAuditAsync(agreementId, paymentId, orderId, path, sha256, AuditOutcome.Success, null);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không lưu được PDF hợp đồng {AgreementId} lên Firebase", agreementId);
                await RecordArchiveAuditAsync(agreementId, paymentId, orderId, path, null, AuditOutcome.Failed, ex.GetType().Name);
                return Result<bool>.Fail(new Error("Agreement.PdfStorageUnavailable", "Thanh toán đã thành công nhưng chưa lưu được PDF hợp đồng. Hệ thống sẽ thử tạo lại khi có yêu cầu xem."));
            }
        }

        /// <summary>Trả PDF cho người tham gia giao dịch; moderator/admin chỉ được xem khi đơn có dispute đang hoạt động.</summary>
        public async Task<Result<byte[]>> GetPaidAgreementPdfAsync(Guid agreementId, Guid currentUserId, bool canModerate, CancellationToken cancellationToken = default)
        {
            var agreement = await _agreementRepo.GetByIdAsync(agreementId, cancellationToken);
            if (agreement == null)
                return Result<byte[]>.Fail(new Error("Agreement.NotFound", "Không tìm thấy hợp đồng."));

            var isParticipant = agreement.BuyerId == currentUserId || agreement.SellerId == currentUserId;
            var order = await _orderRepo.GetByAgreementIdAsync(agreementId, cancellationToken);
            if (!isParticipant)
            {
                if (!canModerate || order == null || !await _disputeRepo.ExistsActiveAsync(DisputeTargetType.Order, order.OrderId, cancellationToken))
                    return Result<byte[]>.Fail(new Error("Agreement.PdfForbidden", "Bạn không có quyền xem PDF hợp đồng này."));
            }

            var payment = (await _paymentRepo.GetByAgreementAsync(agreementId, cancellationToken))
                .Where(IsPaidPayment)
                .OrderByDescending(x => x.PaidAt)
                .FirstOrDefault();
            if (payment == null || order == null)
                return Result<byte[]>.Fail(new Error("Agreement.PdfPaymentNotCompleted", "Chỉ có thể xem PDF sau khi thanh toán thành công."));

            var path = GetStoragePath(agreementId);
            try
            {
                var pdf = await _fileStorageService.DownloadFileAsync(path, cancellationToken);
                if (pdf == null || pdf.Length == 0)
                {
                    var archiveResult = await ArchivePaidAgreementPdfAsync(agreementId, payment.PaymentId, order.OrderId, cancellationToken);
                    if (!archiveResult.IsSuccess)
                        return Result<byte[]>.Fail(archiveResult.Error!);
                    pdf = await _fileStorageService.DownloadFileAsync(path, cancellationToken);
                }

                if (pdf == null || pdf.Length == 0)
                    return Result<byte[]>.Fail(new Error("Agreement.PdfStorageUnavailable", "Chưa lấy được PDF hợp đồng. Vui lòng thử lại sau."));

                try
                {
                    await _auditService.EnqueueAsync(new AuditEvent
                    {
                        Category = AuditCategory.BusinessOperation,
                        Action = AuditActions.AgreementPdfRead,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.User,
                        UserId = currentUserId,
                        TargetType = AuditTargetTypes.Agreement,
                        TargetId = agreementId,
                        Source = AuditSource.HttpApi,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["orderId"] = order.OrderId,
                            ["moderatorAccess"] = !isParticipant,
                            ["storagePath"] = path
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không ghi được audit tải PDF hợp đồng {AgreementId}", agreementId);
                }

                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đọc được PDF hợp đồng {AgreementId} từ Firebase", agreementId);
                return Result<byte[]>.Fail(new Error("Agreement.PdfStorageUnavailable", "Chưa lấy được PDF hợp đồng. Vui lòng thử lại sau."));
            }
        }

        private byte[] GeneratePdf(agreement_form agreement, payment payment, payment_transaction? transaction, order order)
        {
            AgreementDetailsDto? details = null;
            AgreementProductSnapshot? product = null;
            if (!string.IsNullOrWhiteSpace(agreement.AgreementDetailsJsonb))
                details = JsonSerializer.Deserialize<AgreementDetailsDto>(agreement.AgreementDetailsJsonb);
            if (!string.IsNullOrWhiteSpace(agreement.PSnapshot))
                product = JsonSerializer.Deserialize<AgreementProductSnapshot>(agreement.PSnapshot);

            var productName = product?.ProductInfo?.ProductName;
            if (string.IsNullOrWhiteSpace(productName)) productName = order.ProductName ?? "Sản phẩm HomeCycle";
            var buyer = details?.BuyerInfo;
            var seller = details?.SellerInfo;
            var deliveryDate = details?.InspectionDate ?? details?.CollectionDate;
            var deliveryAddress = details?.InspectionAddress ?? details?.DeliveryAddress ?? details?.PickupAddress;
            var totalAmount = order.FinalTotalAmount ?? ((agreement.FinalPrice ?? agreement.InitialPrice ?? 0) * agreement.Quantity + (details?.EstimatedShippingFee ?? 0));

            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(34);
                page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(9).FontColor("#253044"));
                page.Header().Column(column =>
                {
                    column.Item().Text("HOMECYCLE").FontSize(18).Bold().FontColor("#0F766E");
                    column.Item().PaddingTop(4).Text("HỢP ĐỒNG GIAO DỊCH ĐÃ THANH TOÁN").FontSize(14).Bold();
                    column.Item().PaddingTop(3).Text($"Mã hợp đồng: {agreement.AgreementId}").FontColor("#64748B");
                    column.Item().Text($"Ngày tạo: {agreement.CreatedAt:dd/MM/yyyy HH:mm} UTC • Người mua xác nhận: {FormatDate(agreement.BuyerConfirmedAt)} • Người bán xác nhận: {FormatDate(agreement.SellerConfirmedAt)}")
                        .FontSize(8).FontColor("#64748B");
                });
                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(11);
                    column.Item().Element(Section).Text("Thông tin giao dịch");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });
                        AddCell(table, "Mã đơn hàng", order.OrderCode, true);
                        AddCell(table, "Mã thanh toán", payment.PaymentId.ToString());
                        AddCell(table, "Mã giao dịch PayOS", transaction?.PayOSTransactionId ?? "Không áp dụng", true);
                        AddCell(table, "Ngày thanh toán", payment.PaidAt?.ToString("dd/MM/yyyy HH:mm 'UTC'") ?? "Không có");
                    });

                    column.Item().Element(Section).Text("Các bên giao dịch");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(3);
                        });
                        AddCell(table, "Người mua", buyer?.FullName ?? "Chưa có thông tin", true);
                        AddCell(table, "Điện thoại", buyer?.Phone ?? "Chưa có thông tin");
                        AddCell(table, "Địa chỉ", buyer?.FullAddress ?? "Chưa có thông tin", true);
                        AddCell(table, "Người bán", seller?.FullName ?? "Chưa có thông tin");
                        AddCell(table, "Điện thoại", seller?.Phone ?? "Chưa có thông tin", true);
                        AddCell(table, "Địa chỉ", seller?.FullAddress ?? "Chưa có thông tin");
                    });

                    column.Item().Element(Section).Text("Sản phẩm và điều khoản");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(3);
                        });
                        AddCell(table, "Sản phẩm", productName, true);
                        AddCell(table, "Mô tả sản phẩm", product?.PostInfo?.Description);
                        AddCell(table, "Danh mục / loại sản phẩm", JoinValues(product?.ProductInfo?.CategoryName, product?.ProductInfo?.ProductTypeName));
                        AddCell(table, "Thương hiệu / model", JoinValues(product?.ProductInfo?.BrandName, product?.ProductInfo?.ModelNumber));
                        AddCell(table, "Khu vực sử dụng", GetSpaceUsageName(product?.ProductInfo?.SpaceUsage), true);
                        AddCell(table, "Tình trạng hoạt động", GetFunctionalityName(product?.ProductInfo?.FunctionalityStatus));
                        AddCell(table, "Mức độ hư hỏng", GetDamageLevelName(product?.ProductInfo?.DamageLevel), true);
                        AddCell(table, "Thời gian đã sử dụng", product?.ProductInfo?.UsageDuration?.ToString());
                        AddCell(table, "Số lượng", agreement.Quantity.ToString("N0"), true);
                        AddCell(table, "Giá ban đầu", FormatMoney(agreement.InitialPrice));
                        AddCell(table, "Giá đã thống nhất", FormatMoney(agreement.FinalPrice), true);
                        AddCell(table, "Giá gốc tham khảo", FormatMoney(product?.ProductInfo?.OriginalPrice));
                        AddCell(table, "Phí vận chuyển", FormatMoney(details?.EstimatedShippingFee));
                        AddCell(table, "Tổng giá trị đơn", FormatMoney(totalAmount), true);
                        AddCell(table, "Loại giao dịch", agreement.AgreementType == (int)AgreementType.Inspection ? "Có kiểm định" : "Không kiểm định", true);
                        AddCell(table, "Phương thức giao nhận", GetDeliveryMethodName(details?.DeliveryMethod));
                        AddCell(table, "Lịch hẹn", deliveryDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa ghi nhận", true);
                        AddCell(table, "Địa điểm giao nhận", deliveryAddress ?? "Chưa ghi nhận");
                        AddCell(table, "Ghi chú", details?.Notes ?? "Không có", true);
                    });

                    column.Item().Element(Section).Text("Đối chiếu thanh toán");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });
                        AddCell(table, "Trạng thái", "Đã thanh toán thành công", true);
                        AddCell(table, "Phương thức", GetPaymentMethodName(payment.PaymentMethod));
                        AddCell(table, "Số tiền đã thanh toán", FormatMoney(payment.Amount), true);
                        AddCell(table, "Mã giao dịch cổng thanh toán", transaction?.PayOSTransactionId ?? "Không áp dụng");
                    });
                    column.Item().PaddingTop(3).Text("Tài liệu được tạo từ dữ liệu hợp đồng và thanh toán đã lưu trên HomeCycle để hai bên và moderator có căn cứ tra cứu, đối chiếu.")
                        .FontSize(8).FontColor("#64748B");
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("HomeCycle • Tài liệu lưu trữ giao dịch • Trang ");
                    text.CurrentPageNumber();
                }).FontSize(8).FontColor("#64748B");
            })).GeneratePdf();
        }

        private async Task RecordArchiveAuditAsync(Guid agreementId, Guid paymentId, Guid orderId, string path, string? sha256, AuditOutcome outcome, string? errorType)
        {
            try
            {
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.BusinessOperation,
                    Action = AuditActions.AgreementPdfArchive,
                    Outcome = outcome,
                    ActorType = AuditActorType.System,
                    TargetType = AuditTargetTypes.Agreement,
                    TargetId = agreementId,
                    Source = AuditSource.Internal,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["paymentId"] = paymentId,
                        ["orderId"] = orderId,
                        ["storagePath"] = path,
                        ["sha256"] = sha256,
                        ["errorType"] = errorType
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không ghi được audit lưu PDF hợp đồng {AgreementId}", agreementId);
            }
        }

        private static string GetStoragePath(Guid agreementId) => $"{PdfFolder}/{agreementId}/paid-contract.pdf";

        private static string FormatMoney(decimal? amount) => amount.HasValue ? $"{amount.Value:N0} VND" : "Chưa ghi nhận";

        private static string FormatDate(DateTime? value) => value.HasValue ? $"{value.Value:dd/MM/yyyy HH:mm} UTC" : "Chưa xác nhận";

        private static string GetDeliveryMethodName(DeliveryMethod? deliveryMethod) => deliveryMethod switch
        {
            DeliveryMethod.GhnDelivery => "Giao qua GHN",
            DeliveryMethod.SellerDelivers => "Người bán giao",
            DeliveryMethod.BuyerPickUp => "Người mua nhận tại điểm hẹn",
            _ => "Chưa ghi nhận"
        };

        private static string GetPaymentMethodName(int? paymentMethod) => paymentMethod switch
        {
            (int)PaymentMethod.PayOS => "PayOS",
            (int)PaymentMethod.Internal_Wallet => "Ví HomeCycle",
            _ => "Không ghi nhận"
        };

        private static string GetSpaceUsageName(SpaceUsage? spaceUsage) => spaceUsage switch
        {
            SpaceUsage.Living_room => "Phòng khách",
            SpaceUsage.Kitchen => "Nhà bếp",
            SpaceUsage.Bedroom => "Phòng ngủ",
            SpaceUsage.Bathroom => "Phòng tắm",
            SpaceUsage.Laundry_room => "Phòng giặt",
            SpaceUsage.Balcony => "Ban công",
            SpaceUsage.Garage => "Nhà để xe",
            SpaceUsage.Restroom => "Nhà vệ sinh",
            _ => "Chưa ghi nhận"
        };

        private static string GetFunctionalityName(FunctionalityStatus? status) => status switch
        {
            FunctionalityStatus.FullyFunctional => "Hoạt động tốt",
            FunctionalityStatus.PartiallyFunctional => "Hoạt động một phần",
            FunctionalityStatus.NonFunctional => "Không hoạt động",
            _ => "Chưa ghi nhận"
        };

        private static string GetDamageLevelName(DamageLevel? level) => level switch
        {
            DamageLevel.None => "Không hư hại",
            DamageLevel.Cosmetic_Damage => "Hư hại thẩm mỹ",
            DamageLevel.Minor_Damage => "Hư hại nhẹ",
            DamageLevel.Moderate_Damage => "Hư hại trung bình",
            DamageLevel.Severe_Damage => "Hư hại nặng",
            DamageLevel.Total_Loss => "Hư hỏng hoàn toàn",
            _ => "Chưa ghi nhận"
        };

        private static bool IsPaidPayment(payment payment) => payment.PaidAt.HasValue &&
            payment.PaymentStatus is (int)PaymentStatus.Completed or (int)PaymentStatus.Refunded or (int)PaymentStatus.PartiallyRefunded;

        private static string JoinValues(params string?[] values)
        {
            var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToArray();
            return items.Length == 0 ? "Chưa ghi nhận" : string.Join(" / ", items);
        }

        private static IContainer Section(IContainer container) => container
            .Background("#E6F4F1")
            .PaddingVertical(6)
            .PaddingHorizontal(8);

        private static void AddCell(TableDescriptor table, string label, string? value, bool alternate = false)
        {
            var background = alternate ? "#F8FAFC" : "#FFFFFF";
            table.Cell().Background(background).Padding(6).Text(label).SemiBold();
            table.Cell().Background(background).Padding(6).Text(string.IsNullOrWhiteSpace(value) ? "Chưa ghi nhận" : value);
        }
    }
}
