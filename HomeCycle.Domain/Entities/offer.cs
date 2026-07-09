using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public int? OfferStatus { get; set; }

    public DateTime CreatedAt { get; set; }

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
