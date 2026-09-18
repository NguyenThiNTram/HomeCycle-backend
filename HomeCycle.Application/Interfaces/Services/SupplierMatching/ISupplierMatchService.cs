using HomeCycle.Application.DTOs.Requests.SupplierMatching;
using HomeCycle.Application.DTOs.Responses.SupplierMatching;
using HomeCycle.Application.SupplierMatching.Models;

namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchService
{
    Task<SupplierMatchResponse> MatchDraftAsync(
        Guid requesterId,
        SupplierMatchDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierMatchResponse> MatchAsync(
        SupplierDemandContext demand,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<SupplierMatchResponse> MatchBackendOnlyAsync(
        SupplierDemandContext demand,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<SupplierMatchResponse> MatchInitialBackendAsync(
        SupplierDemandContext demand,
        int take,
        CancellationToken cancellationToken = default);
}
