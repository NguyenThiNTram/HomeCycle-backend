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
    }

    public interface IGhnService
    {
        Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

        Task<GhnFeeQuoteResponse> GetShippingFeeAsync(CalculateGhnFeeRequest request, CancellationToken cancellationToken = default);

        Task<GhnPreviewQuote> PreviewOrderAsync(GhnShippingPreviewRequest request, CancellationToken cancellationToken = default);

        Task<GhnCreateOrderResponse> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<GhnOrderDetailResponse> GetOrderDetailAsync(string ghnOrderCode, CancellationToken cancellationToken = default);
    }
}
