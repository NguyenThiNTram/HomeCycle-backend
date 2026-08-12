using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Agreements
{
    public interface IAgreementFormRepository
    {
        Task<agreement_form?> GetByNegotiationIdAsync(Guid negotiationId, CancellationToken cancellationToken = default);
        Task<agreement_form?> GetByIdAsync(Guid agreementId, CancellationToken cancellationToken = default);
        Task AddAsync(agreement_form agreement, CancellationToken cancellationToken = default);
        Task UpdateAsync(agreement_form agreement, CancellationToken cancellationToken = default);
        Task<PagedResult<PendingAgreementListItemDto>> GetPendingPaymentByBuyerAsync(
            Guid buyerId,
            PendingAgreementSearchRequest request,
            CancellationToken cancellationToken = default);

    }
}
