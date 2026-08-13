using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Externals
{
    public interface IPayoutGatewayService
    {
        Task<Result<GatewayPayoutResponse>> CreatePayoutAsync(GatewayPayoutRequest request, CancellationToken ct = default);
        Task<Result<GatewayPayoutStatusResponse>> GetPayoutStatusAsync(string referenceId, CancellationToken ct = default);
    }
}
