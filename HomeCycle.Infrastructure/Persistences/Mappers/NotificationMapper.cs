using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class NotificationMapper
    {
        public static notification ToDomain(this Notification entity)
        {
            if (entity == null) return null;
            return new notification
            {
                NotificationId = entity.NotificationId,
                UserId = entity.UserId,
                Title = entity.Title,
                Message = entity.Message,
                TargetType = entity.TargetType,
                TargetId = entity.TargetId,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Notification ToInfrastructure(this notification entity)
        {
            if (entity == null) return null;
            return new Notification
            {
                NotificationId = entity.NotificationId,
                UserId = entity.UserId,
                Title = entity.Title,
                Message = entity.Message,
                TargetType = entity.TargetType,
                TargetId = entity.TargetId,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
