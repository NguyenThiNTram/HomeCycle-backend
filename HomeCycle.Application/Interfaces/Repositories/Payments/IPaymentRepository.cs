using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Payments
{
    public interface IPaymentRepository
    {
        Task<payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default);
        Task<payment?> GetLatestPendingByAgreementAsync(Guid agreementId, CancellationToken ct = default); 
        Task AddAsync(payment payment, CancellationToken ct = default);
        Task UpdateAsync(payment payment, CancellationToken ct = default);
        Task<PagedResult<PaymentHistoryResponseDto>> GetPagedPaymentHistoryAsync(Guid userId, PaymentHistorySearchRequest request, CancellationToken ct = default);
        Task<payment?> GetLatestPaidByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    }
}
