using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;

public class ghn_shipment
{
    public Guid GHNShipmentId { get; set; }
    public Guid ShipmentId { get; set; }

    public string? GHNOrderCode { get; set; }
    public string? ClientOrderCode { get; set; }
    public string? GHNStatusCode { get; set; }

    public int? ServiceId { get; set; }
    public int? ServiceTypeId { get; set; }
    public int? FromDistrictId { get; set; }
    public string? FromWardCode { get; set; }
    public int? ToDistrictId { get; set; }
    public string? ToWardCode { get; set; }

    public int? Weight { get; set; }
    public int? Length { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    public int? CODAmount { get; set; }
    public int PaymentTypeId { get; set; }
    public int InsuranceValue { get; set; }
    public int? RequiredNote { get; set; }
    public decimal? GHNServiceFee { get; set; }
    public decimal? GHNCodFee { get; set; }
    public decimal? GHNTotalFee { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    public ghn_shipment()
    {
    }

    public ghn_shipment(Guid GHNShipmentId, Guid ShipmentId)
    {
        this.GHNShipmentId = GHNShipmentId;
        this.ShipmentId = ShipmentId;
    }
}
