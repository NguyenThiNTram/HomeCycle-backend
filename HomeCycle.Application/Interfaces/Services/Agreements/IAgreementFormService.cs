using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.DTOs.Responses.GHN;
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
        Task<Result<AgreementActionResponse>> UpdateAgreementAsync(Guid agreementId, UpdateAgreementFormRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<AgreementActionResponse>> AcceptAgreementAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<AgreementActionResponse>> RequestEditAsync(Guid agreementId, Guid currentUserId, CancellationToken cancellationToken = default);
        Task<Result<ShippingFeePreviewResponse>> PreviewShippingFeeAsync(Guid negotiationId, Guid currentUserId, CalculateGhnFeeRequest request, CancellationToken cancellationToken = default);
    }
}
