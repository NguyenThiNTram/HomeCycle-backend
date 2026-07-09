using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Negotiation")]
[Index("OfferId", Name = "Negotiation_OfferId_key", IsUnique = true)]
[Index("BuyerId", Name = "idx_negotiation_buyer")]
[Index("OfferId", Name = "idx_negotiation_offer")]
[Index("PostId", Name = "idx_negotiation_post")]
[Index("SellerId", Name = "idx_negotiation_seller")]
public partial class Negotiation
{
    [Key]
    public Guid NegotiationId { get; set; }

    public Guid PostId { get; set; }

    public Guid OfferId { get; set; }

    public Guid SellerId { get; set; }

    public Guid BuyerId { get; set; }

    [Precision(18, 2)]
    public decimal? FinalPrice { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? NegotiationStatus { get; set; }

    [InverseProperty("Negotiation")]
    public virtual Agreement_Form? Agreement_Form { get; set; }

    [ForeignKey("BuyerId")]
    [InverseProperty("NegotiationBuyers")]
    public virtual User Buyer { get; set; } = null!;

    [InverseProperty("Negotiation")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [ForeignKey("OfferId")]
    [InverseProperty("Negotiation")]
    public virtual Offer Offer { get; set; } = null!;

    [ForeignKey("PostId")]
    [InverseProperty("Negotiations")]
    public virtual Post Post { get; set; } = null!;

    [ForeignKey("SellerId")]
    [InverseProperty("NegotiationSellers")]
    public virtual User Seller { get; set; } = null!;
}
