using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Agreement_Form")]
[Index("NegotiationId", Name = "Agreement_Form_NegotiationId_key", IsUnique = true)]
[Index("BuyerId", Name = "idx_agreement_buyer")]
[Index("PostId", Name = "idx_agreement_post")]
[Index("SellerId", Name = "idx_agreement_seller")]
public partial class Agreement_Form
{
    [Key]
    public Guid AgreementId { get; set; }

    public Guid NegotiationId { get; set; }

    public Guid PostId { get; set; }

    public Guid SellerId { get; set; }

    public Guid BuyerId { get; set; }

    [Column(TypeName = "json")]
    public string? PSnapshot { get; set; }

    public int Quantity { get; set; }

    [Precision(18, 2)]
    public decimal? InitialPrice { get; set; }

    [Precision(18, 2)]
    public decimal? FinalPrice { get; set; }

    public int? AgreementType { get; set; }

    [Column(TypeName = "json")]
    public string? AgreementDetailsJsonb { get; set; }

    public int? PaymentType { get; set; }

    public int? AgreementStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? BuyerConfirmedAt { get; set; }

    public DateTime? SellerConfirmedAt { get; set; }

    [InverseProperty("Agreement")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [ForeignKey("BuyerId")]
    [InverseProperty("Agreement_FormBuyers")]
    public virtual User Buyer { get; set; } = null!;

    [ForeignKey("NegotiationId")]
    [InverseProperty("Agreement_Form")]
    public virtual Negotiation Negotiation { get; set; } = null!;

    [InverseProperty("Agreement")]
    public virtual Order? Order { get; set; }

    [InverseProperty("Agreement")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("PostId")]
    [InverseProperty("Agreement_Forms")]
    public virtual Post Post { get; set; } = null!;

    [ForeignKey("SellerId")]
    [InverseProperty("Agreement_FormSellers")]
    public virtual User Seller { get; set; } = null!;
}
