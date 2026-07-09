using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;
public class agreement_form
{
    public Guid AgreementId { get; set; }
    public Guid NegotiationId { get; set; }
    public Guid PostId { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }

    public string? PSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal? InitialPrice { get; set; }
    public decimal? FinalPrice { get; set; }

    public int? AgreementType { get; set; }
    public string? AgreementDetailsJsonb { get; set; }
    public int? PaymentType { get; set; }
    public int? AgreementStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? BuyerConfirmedAt { get; set; }
    public DateTime? SellerConfirmedAt { get; set; }

    public agreement_form()
    {
    }

    public agreement_form(
        Guid agreementId,
        Guid negotiationId,
        Guid postId,
        Guid sellerId,
        Guid buyerId,
        int quantity)
    {
        AgreementId = agreementId;
        NegotiationId = negotiationId;
        PostId = postId;
        SellerId = sellerId;
        BuyerId = buyerId;
        Quantity = quantity;
        CreatedAt = DateTime.UtcNow;
    }

}
