using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class order
{
    public Guid OrderId { get; set; }
    public Guid AgreementId { get; set; }
    public Guid PostId { get; set; }

    public string? ProductName { get; set; }
    public int Quantity { get; set; }

    public decimal? OriginalTotalAmount { get; set; }
    public decimal? FinalTotalAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public decimal? AmountRemaining { get; set; }

    public int? PaymentStatus { get; set; }
    public int? OrderStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public order()
    {
    }

    public order(Guid OrderId, Guid AgreementId, Guid PostId)
    {
        this.OrderId = OrderId;
        this.AgreementId = AgreementId;
        this.PostId = PostId;
    }
}
