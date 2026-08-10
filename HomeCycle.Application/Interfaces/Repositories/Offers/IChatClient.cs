using HomeCycle.Application.DTOs.Responses.Negotiations;
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

        Task ConversationUpdated(ConversationUpdatedResponse response);
    }
}
