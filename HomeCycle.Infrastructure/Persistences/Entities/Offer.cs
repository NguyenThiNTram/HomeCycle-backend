using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Offer")]
[Index("PostId", Name = "idx_offer_post")]
[Index("ReceiverId", "OfferStatus", Name = "idx_offer_receiver_status")]
[Index("SenderId", "OfferStatus", Name = "idx_offer_sender_status")]
[Index("OfferStatus", Name = "idx_offer_status")]
public partial class Offer
{
    [Key]
    public Guid OfferId { get; set; }

    public Guid PostId { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    [Precision(18, 2)]
    public decimal? OfferPrice { get; set; }

    public int OfferQuantity { get; set; }

    public int? OfferStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Offer")]
    public virtual Negotiation? Negotiation { get; set; }

    [ForeignKey("PostId")]
    [InverseProperty("Offers")]
    public virtual Post Post { get; set; } = null!;

    [ForeignKey("ReceiverId")]
    [InverseProperty("OfferReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("OfferSenders")]
    public virtual User Sender { get; set; } = null!;
}
