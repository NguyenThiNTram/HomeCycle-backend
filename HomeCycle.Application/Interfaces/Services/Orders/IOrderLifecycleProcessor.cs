using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Orders
{
    public interface IOrderLifecycleProcessor
    {
        string Name { get; }

        Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken ct = default);
    }
}
