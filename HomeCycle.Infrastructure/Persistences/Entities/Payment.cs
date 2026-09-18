using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Payment")]
[Index("AgreementId", Name = "idx_payment_agreement")]
[Index("OrderId", Name = "idx_payment_order")]
[Index("PayerId", Name = "idx_payment_payer")]
[Index("SubscriptionId", Name = "ux_payment_subscription", IsUnique = true)]
public partial class Payment
{
    [Key]
    public Guid PaymentId { get; set; }

    public Guid? AgreementId { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? SubscriptionId { get; set; }

    public Guid PayerId { get; set; }

    public int? PaymentType { get; set; }

    public int? PaymentMethod { get; set; }

    [Precision(18, 2)]
    public decimal? Amount { get; set; }

    public string? Description { get; set; }

    public int? PaymentStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    [ForeignKey("AgreementId")]
    [InverseProperty("Payments")]
    public virtual Agreement_Form? Agreement { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Payments")]
    public virtual Order? Order { get; set; }

    [ForeignKey("SubscriptionId")]
    [InverseProperty("Payment")]
    public virtual User_Subscription? Subscription { get; set; }

    [ForeignKey("PayerId")]
    [InverseProperty("Payments")]
    public virtual User Payer { get; set; } = null!;

    [InverseProperty("Payment")]
    public virtual ICollection<Payment_Transaction> Payment_Transactions { get; set; } = new List<Payment_Transaction>();

    [InverseProperty("Payment")]
    public virtual ICollection<Wallet_Transaction> Wallet_Transactions { get; set; } = new List<Wallet_Transaction>();
}
