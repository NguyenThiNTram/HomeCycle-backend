using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Negotiates
{
    public interface IAgreementFormService
    {
        Task CreateFromNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken = default);
    }
}
