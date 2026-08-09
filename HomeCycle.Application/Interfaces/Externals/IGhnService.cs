using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Application.DTOs.Responses.Shippings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Externals
{
    public interface IGhnService
    {
        Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

        Task<GhnFeeQuoteResponse> GetShippingFeeAsync(GhnFeeQuoteRequest request, CancellationToken cancellationToken = default);
    }
}
