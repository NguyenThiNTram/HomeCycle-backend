using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Disputes
{
    public interface IDisputeWindowPolicy
    {
        Task<TimeSpan> GetOrderDisputeWindowAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default);
    }
}
