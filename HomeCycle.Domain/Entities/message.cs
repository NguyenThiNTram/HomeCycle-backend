using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class message
{
    public Guid MessageId { get; set; }
    public Guid NegotiationId { get; set; }
    public Guid SenderId { get; set; }

    public string? MessageContent { get; set; }
    public int? MessageType { get; set; }

    public decimal? OfferPrice { get; set; }
    public int OfferQuantity { get; set; }
    public int? OfferStatus { get; set; }


    public string? MediaUrl { get; set; }
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public message()
    {
    }

    public message(Guid MessageId, Guid NegotiationId, Guid SenderId)
    {
        this.MessageId = MessageId;
        this.NegotiationId = NegotiationId;
        this.SenderId = SenderId;
    }
}
