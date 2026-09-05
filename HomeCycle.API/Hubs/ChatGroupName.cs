namespace HomeCycle.API.Hubs
{
    public class ChatGroupName
    {
        public static string ForNegotiation(Guid negotiationId) => $"negotiation:{negotiationId:N}";
        public static string ForConversation(Guid conversationId) => $"conversation:{conversationId:N}";
    }
}
