using HomeCycle.Application.Commons.Results;

namespace HomeCycle.Application.Interfaces.Services.Agreements
{
    public interface IAgreementPdfService
    {
        /// <summary>Lưu bản PDF bất biến của hợp đồng sau khi payment và order đã được xác nhận thành công.</summary>
        Task<Result<bool>> ArchivePaidAgreementPdfAsync(Guid agreementId, Guid paymentId, Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>Lấy PDF cho hai bên giao dịch hoặc moderator/admin khi đơn hàng có dispute đang hoạt động.</summary>
        Task<Result<byte[]>> GetPaidAgreementPdfAsync(Guid agreementId, Guid currentUserId, bool canModerate, CancellationToken cancellationToken = default);
    }
}
