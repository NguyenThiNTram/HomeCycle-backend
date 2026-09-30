using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests;
using HomeCycle.Application.DTOs.Responses;

namespace HomeCycle.Application.Interfaces.Services.AI;

public interface IIdentityDocumentScanService
{
    Task<Result<IdentityDocumentScanResponse>> ScanAsync(
        IdentityDocumentScanRequest request,
        CancellationToken cancellationToken = default);
}
