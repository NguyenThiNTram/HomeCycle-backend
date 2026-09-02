using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Entities
{
    public class conversation
    {
        public Guid ConversationId { get; set; }
        public Guid UserOneId { get; set; }
        public Guid UserTwoId { get; set; }

        public DateTime LastActivityAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual user? UserOne { get; set; }
        public virtual user? UserTwo { get; set; }

        public conversation()
        {
        }

        public conversation(
            Guid conversationId,
            Guid userOneId,
            Guid userTwoId)
        {
            ConversationId = conversationId;
            UserOneId = userOneId;
            UserTwoId = userTwoId;
        }
    }
}
