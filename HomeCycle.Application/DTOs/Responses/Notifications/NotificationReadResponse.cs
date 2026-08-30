using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Notifications
{
    public class NotificationReadResponse
    {
        public Guid NotificationId { get; set; }
        public bool IsRead { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationsReadAllResponse
    {
        public int UpdatedCount { get; set; }
        public int UnreadCount { get; set; }
    }
}
