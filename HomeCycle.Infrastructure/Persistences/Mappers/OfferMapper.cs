using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class OfferMapper
    {
        public static offer ToDomain(this Offer entity)
        {
            if (entity == null) return null;
            return new offer
            {
                OfferId = entity.OfferId,
                PostId = entity.PostId,
                SenderId = entity.SenderId,
                ReceiverId = entity.ReceiverId,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = entity.OfferStatus,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Offer ToInfrastructure(this offer entity)
        {
            if (entity == null) return null;
            return new Offer
            {
                OfferId = entity.OfferId,
                PostId = entity.PostId,
                SenderId = entity.SenderId,
                ReceiverId = entity.ReceiverId,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = entity.OfferStatus,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
