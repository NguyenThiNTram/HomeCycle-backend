using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Notifications
{
    public class NotificationResponse
    {
        public Guid NotificationId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public NotificationTargetType? TargetType { get; set; }
        public Guid? TargetId { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    //Command dùng nội bộ
    public sealed record CreateNotificationCommand(Guid UserId, string Title, string Message, NotificationTargetType TargetType, Guid TargetId);
}
