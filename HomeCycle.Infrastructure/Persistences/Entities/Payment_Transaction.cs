using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Payment_Transaction")]
[Index("PaymentId", Name = "idx_payment_transaction_payment")]
[Index("UserId", Name = "idx_payment_transaction_user")]
[Index("PayOSOrderCode", Name = "uq_payos_order", IsUnique = true)]
[Index("PayOSTransactionId", Name = "uq_payos_transaction", IsUnique = true)]
[Index("PayOSTransactionId", Name = "ux_payment_transaction_id", IsUnique = true)]
[Index("PayOSOrderCode", Name = "ux_payment_transaction_order", IsUnique = true)]
public partial class Payment_Transaction
{
    [Key]
    public Guid PaymentTransactionId { get; set; }

    public Guid PaymentId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string? PayOSOrderCode { get; set; }

    [StringLength(100)]
    public string? PayOSPaymentLinkId { get; set; }

    [StringLength(100)]
    public string? PayOSTransactionId { get; set; }

    [StringLength(500)]
    public string? CheckoutUrl { get; set; }

    public int? PaymentTransactionStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("PaymentId")]
    [InverseProperty("Payment_Transactions")]
    public virtual Payment Payment { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Payment_Transactions")]
    public virtual User User { get; set; } = null!;
}
