using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Order")]
[Index("AgreementId", Name = "Order_AgreementId_key", IsUnique = true)]
[Index("AgreementId", Name = "idx_order_agreement")]
[Index("PaymentStatus", Name = "idx_order_payment_status")]
[Index("PostId", Name = "idx_order_post")]
[Index("OrderStatus", Name = "idx_order_status")]
public partial class Order
{
    [Key]
    public Guid OrderId { get; set; }

    public Guid AgreementId { get; set; }

    public Guid PostId { get; set; }

    [StringLength(255)]
    public string? ProductName { get; set; }

    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal? OriginalTotalAmount { get; set; }

    [Precision(18, 2)]
    public decimal? FinalTotalAmount { get; set; }

    [Precision(18, 2)]
    public decimal? AmountPaid { get; set; }

    [Precision(18, 2)]
    public decimal? AmountRemaining { get; set; }

    public int? PaymentStatus { get; set; }

    public int? OrderStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AgreementId")]
    [InverseProperty("Order")]
    public virtual Agreement_Form Agreement { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("PostId")]
    [InverseProperty("Orders")]
    public virtual Post Post { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual Review? Review { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
