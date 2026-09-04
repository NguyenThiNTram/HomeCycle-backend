using HomeCycle.Application.DTOs.Responses.Conversations;
using HomeCycle.Application.DTOs.Responses.Messages;
using HomeCycle.Application.DTOs.Responses.Notifications;
using HomeCycle.Application.DTOs.Responses.Offers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IChatClient
    {
        Task MessageCreated(MessageResponse message);

        Task MessageUpdated(MessageResponse message);

        Task MessagesRead(MessagesReadResponse response);

        // Conversation
        Task ConversationMessageCreated(MessageResponse message);

        Task ConversationMessageUpdated(MessageResponse message);

        Task ConversationMessagesRead(ConversationMessagesReadResponse response);

        Task ConversationUpdated(ConversationUpdatedResponse response);

        // Offer
        Task OfferCreated(OfferResponse offer);

        Task OfferUpdated(OfferResponse offer);

        // Notification
        Task NotificationCreated(NotificationResponse notification);

        Task NotificationRead(NotificationReadResponse response);

        Task NotificationsReadAll(NotificationsReadAllResponse response);


    }
}
