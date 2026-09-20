using HomeCycle.Application.DTOs.Responses.Carts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Carts
{
    public interface ICartRealtimePublisher
    {
        Task PublishUpdatedAsync(Guid userId, CartUpdatedResponse response, CancellationToken cancellationToken = default);
    }
}
