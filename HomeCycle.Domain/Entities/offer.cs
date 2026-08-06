using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class offer
{
    public Guid OfferId { get; set; }
    public Guid PostId { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }


    public decimal? OfferPrice { get; set; }
    public int OfferQuantity { get; set; }
    public OfferStatus? OfferStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual post? Post { get; set; } = null!;

    public virtual user? Sender { get; set; } = null!;
    public virtual user? Receiver { get; set; } = null!;

    public offer()
    {
    }

    public offer(Guid OfferId, Guid PostId, Guid SenderId, Guid ReceiverId)
    {
        this.OfferId = OfferId;
        this.PostId = PostId;
        this.SenderId = SenderId;
        this.ReceiverId = ReceiverId;
    }

}
