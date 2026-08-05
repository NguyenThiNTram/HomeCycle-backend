using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Agreements
{
    public interface IAgreementFormService
    {
        Task<Result<AgreementPreviewResponse>> GetPreviewAsync(Guid negotiationId, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<Guid>> CreateAgreementAsync(CreateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<AgreementDetailResponse>> GetDetailAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateAgreementAsync(Guid agreementId, UpdateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<bool>> AcceptAgreementAsync(Guid agreementId, Guid buyerId, CancellationToken cancellationToken = default);
    }
}
