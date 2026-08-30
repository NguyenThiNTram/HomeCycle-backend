using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Domain.Entities;

public class notification
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }

    public string? Title { get; set; }
    public string? Message { get; set; }

    public NotificationTargetType? TargetType { get; set; }
    public Guid? TargetId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public user? User { get; set; }

    public notification()
    {
    }

    public notification(Guid NotificationId, Guid UserId)
    {
        this.NotificationId = NotificationId;
        this.UserId = UserId;
    }
}
