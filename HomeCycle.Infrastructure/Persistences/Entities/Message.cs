using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Index("CreatedAt", Name = "idx_message_created")]
[Index("NegotiationId", Name = "idx_message_negotiation")]
[Index("SenderId", Name = "idx_message_sender")]
public partial class Message
{
    [Key]
    public Guid MessageId { get; set; }

    public Guid NegotiationId { get; set; }

    public Guid SenderId { get; set; }

    public string? MessageContent { get; set; }

    public int? MessageType { get; set; }

    [Precision(18, 2)]
    public decimal? OfferPrice { get; set; }

    public int OfferQuantity { get; set; }

    public int? OfferStatus { get; set; }

    [StringLength(500)]
    public string? MediaUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("NegotiationId")]
    [InverseProperty("Messages")]
    public virtual Negotiation Negotiation { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("Messages")]
    public virtual User Sender { get; set; } = null!;
}
