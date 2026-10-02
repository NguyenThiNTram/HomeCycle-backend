using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.Persistences.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class DisputeResponseMapper
    {
        public static dispute_response ToDomain(this Dispute_Response entity)
        {
            return new dispute_response
            {
                DisputeResponseId = entity.DisputeResponseId,
                DisputeId = entity.DisputeId,
                ResponderId = entity.ResponderId,
                ResponseType = entity.ResponseType,
                Content = entity.Content,
                CreatedAt = entity.CreatedAt
            };
        }

        public static Dispute_Response ToInfrastructure(this dispute_response entity)
        {
            return new Dispute_Response
            {
                DisputeResponseId = entity.DisputeResponseId,
                DisputeId = entity.DisputeId,
                ResponderId = entity.ResponderId,
                ResponseType = entity.ResponseType,
                Content = entity.Content,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
