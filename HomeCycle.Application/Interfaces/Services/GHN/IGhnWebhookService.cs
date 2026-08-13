using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.GHN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.GHN
{
    public interface IGhnWebhookService
    {
        Task<Result> ProcessAsync(GhnWebhookRequest request, CancellationToken cancellationToken = default);
    }
}
