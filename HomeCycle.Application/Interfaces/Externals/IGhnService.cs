using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Externals
{
    /// <summary>
    /// Đánh dấu lỗi do GHN trả về (đã có phản hồi chính thức => call definitively failed).
    /// Giúp tầng Application phân loại lỗi mà không cần phụ thuộc vào Infrastructure.
    /// </summary>
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
    }
}
