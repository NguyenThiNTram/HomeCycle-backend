using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.Persistences.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class ConversationMapper
    {
        public static conversation? ToDomain(this Conversation? entity)
        {
            if (entity is null)
                return null;

            return new conversation
            {
                ConversationId = entity.ConversationId,
                UserOneId = entity.UserOneId,
                UserTwoId = entity.UserTwoId,
                LastActivityAt = entity.LastActivityAt,
                CreatedAt = entity.CreatedAt,

                UserOne = entity.UserOne?.ToDomain(),
                UserTwo = entity.UserTwo?.ToDomain()
            };
        }

        public static Conversation? ToInfrastructure(
            this conversation? entity)
        {
            if (entity is null)
                return null;

            return new Conversation
            {
                ConversationId = entity.ConversationId,
                UserOneId = entity.UserOneId,
                UserTwoId = entity.UserTwoId,
                LastActivityAt = entity.LastActivityAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
