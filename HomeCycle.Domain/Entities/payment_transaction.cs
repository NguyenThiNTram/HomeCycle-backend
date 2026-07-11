using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class payment_transaction
{
    public Guid PaymentTransactionId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }

    public string? PayOSOrderCode { get; set; }
    public string? PayOSPaymentLinkId { get; set; }
    public string? PayOSTransactionId { get; set; }
    public string? CheckoutUrl { get; set; }

    public int? PaymentTransactionStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public payment_transaction()
    {
    }

    public payment_transaction(Guid PaymentTransactionId, Guid PaymentId, Guid UserId)
    {
        this.PaymentTransactionId = PaymentTransactionId;
        this.PaymentId = PaymentId;
        this.UserId = UserId;
    }
}
