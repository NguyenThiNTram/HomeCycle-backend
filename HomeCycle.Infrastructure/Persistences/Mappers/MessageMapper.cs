using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class MessageMapper
    {
        public static message ToDomain(this Message entity)
        {
            return new message
            {
                MessageId = entity.MessageId,
                NegotiationId = entity.NegotiationId,
                SenderId = entity.SenderId,
                MessageContent = entity.MessageContent,
                MessageType = entity.MessageType,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = entity.OfferStatus,
                MediaUrl = entity.MediaUrl,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Message ToInfrastructure(this message entity)
        {
            return new Message
            {
                MessageId = entity.MessageId,
                NegotiationId = entity.NegotiationId,
                SenderId = entity.SenderId,
                MessageContent = entity.MessageContent,
                MessageType = entity.MessageType,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = entity.OfferStatus,
                MediaUrl = entity.MediaUrl,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
