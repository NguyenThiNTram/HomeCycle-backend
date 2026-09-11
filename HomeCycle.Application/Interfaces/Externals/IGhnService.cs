using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Externals
{
    public interface IGhnApiError
    {
        string? CodeMessage { get; }
        int HttpStatusCode { get; }
    }

    public interface IGhnService
    {
        Task<GhnLeadtimeResponse> GetLeadtimeAsync(GhnLeadtimeRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GhnCancelOrderResponse>> CancelOrdersAsync(IReadOnlyList<string> orderCodes,
            string? reasonCode = null, string? reason = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GhnAvailableServiceResponse>> GetAvailableServicesAsync(int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

        Task<GhnFeeQuoteResponse> GetShippingFeeAsync(CalculateGhnFeeRequest request, CancellationToken cancellationToken = default);

        Task<GhnPreviewQuote> PreviewOrderAsync(GhnShippingPreviewRequest request, CancellationToken cancellationToken = default);

        Task<GhnCreateOrderResponse> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<GhnOrderDetailResponse> GetOrderDetailAsync(string ghnOrderCode, CancellationToken cancellationToken = default);
    }
}
