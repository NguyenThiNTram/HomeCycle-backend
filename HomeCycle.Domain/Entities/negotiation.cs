using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Domain.Entities;

public partial class negotiation
{
    public Guid NegotiationId { get; set; }
    public Guid PostId { get; set; }
    public Guid OfferId { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }

    public decimal? FinalPrice { get; set; }

    public int? FinalQuantity { get; set; }

    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public NegotiationStatus? NegotiationStatus { get; set; }

    public negotiation()
    {
    }

    public negotiation(Guid NegotiationId, Guid PostId, Guid OfferId, Guid SellerId, Guid BuyerId)
    {
        this.NegotiationId = NegotiationId;
        this.PostId = PostId;
        this.OfferId = OfferId;
        this.SellerId = SellerId;
        this.BuyerId = BuyerId;
    }
}
