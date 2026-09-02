using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class NegotiationMapper
    {
        public static negotiation ToDomain(this Negotiation entity)
        {
            return new negotiation
            {
                NegotiationId = entity.NegotiationId,
                PostId = entity.PostId,
                OfferId = entity.OfferId,
                SellerId = entity.SellerId,
                BuyerId = entity.BuyerId,
                ConversationId = entity.ConversationId,
                FinalPrice = entity.FinalPrice,
                FinalQuantity = entity.FinalQuantity,
                LastMessageAt = entity.LastMessageAt,
                CreatedAt = entity.CreatedAt,
                NegotiationStatus = (NegotiationStatus?)entity.NegotiationStatus,

                Conversation = entity.Conversation?.ToDomain(),
                Post = entity.Post?.ToDomain(),
                Offer = entity.Offer?.ToDomain(),
                Seller = entity.Seller?.ToDomain(),
                Buyer = entity.Buyer?.ToDomain()
            };
        }

        public static Negotiation ToInfrastructure(this negotiation entity)
        {
            return new Negotiation
            {
                NegotiationId = entity.NegotiationId,
                PostId = entity.PostId,
                OfferId = entity.OfferId,
                SellerId = entity.SellerId,
                BuyerId = entity.BuyerId,
                ConversationId = entity.ConversationId,
                FinalPrice = entity.FinalPrice,
                FinalQuantity = entity.FinalQuantity,
                LastMessageAt = entity.LastMessageAt,
                CreatedAt = entity.CreatedAt,
                NegotiationStatus = (int?)entity.NegotiationStatus
            };
        }
    }
}
