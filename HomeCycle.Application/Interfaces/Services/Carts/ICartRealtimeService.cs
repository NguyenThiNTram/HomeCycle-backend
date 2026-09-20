using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Carts
{
    public interface ICartRealtimeService
    {
        Task PublishUpdatedSafelyAsync(Guid userId, DateTime updatedAt);
    }
}
