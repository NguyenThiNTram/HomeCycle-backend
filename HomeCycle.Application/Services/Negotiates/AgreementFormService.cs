using HomeCycle.Application.Interfaces.Services.Negotiates;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Negotiates
{
    public class AgreementFormService : IAgreementFormService
    {
        private readonly ILogger<AgreementFormService> _logger;

        public AgreementFormService(
            ILogger<AgreementFormService> logger)
        {
            _logger = logger;
        }

        public Task CreateFromNegotiationAsync(
            Guid negotiationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (negotiationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "NegotiationId không hợp lệ.",
                    nameof(negotiationId));
            }

            _logger.LogWarning(
                "Tạm thời chưa tạo AgreementForm cho Negotiation {NegotiationId}.",
                negotiationId);

            return Task.CompletedTask;
        }
    }
}
