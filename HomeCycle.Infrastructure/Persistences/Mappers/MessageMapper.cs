using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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
                ConversationId = entity.ConversationId,
                SenderId = entity.SenderId,
                ClientMessageId = entity.ClientMessageId,
                MessageContent = entity.MessageContent,
                MessageType = (MessageType?)entity.MessageType,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = (MessageOfferStatus?)entity.OfferStatus,
                BasePriceSnapshot = entity.BasePriceSnapshot,
                MediaUrl = entity.MediaUrl,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Message ToInfrastructure(this message entity)
        {
            return new Message
            {
                MessageId = entity.MessageId,
                NegotiationId = entity.NegotiationId,
                ConversationId = entity.ConversationId,
                SenderId = entity.SenderId,
                ClientMessageId = entity.ClientMessageId,
                MessageContent = entity.MessageContent,
                MessageType = (int?)entity.MessageType,
                OfferPrice = entity.OfferPrice,
                OfferQuantity = entity.OfferQuantity,
                OfferStatus = (int?)entity.OfferStatus,
                BasePriceSnapshot = entity.BasePriceSnapshot,
                MediaUrl = entity.MediaUrl,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
