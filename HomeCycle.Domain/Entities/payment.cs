using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class payment
{
    public Guid PaymentId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid PayerId { get; set; }

    public int? PaymentType { get; set; }
    public int? PaymentMethod { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }

    public int? PaymentStatus { get; set; }

    public DateTime? PaidAt { get; set; }

    public payment()
    {
    }

    public payment(Guid PaymentId, Guid PayerId)
    {
        this.PaymentId = PaymentId;
        this.PayerId = PayerId;
    }
}
