using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("GHN_Shipment")]
[Index("ShipmentId", Name = "GHN_Shipment_ShipmentId_key", IsUnique = true)]
[Index("ClientOrderCode", Name = "idx_ghn_client_order_code")]
[Index("GHNOrderCode", Name = "uq_ghn_order_code", IsUnique = true)]
[Index("GHNOrderCode", Name = "ux_ghn_order_code", IsUnique = true)]
public partial class GHN_Shipment
{
    [Key]
    public Guid GHNShipmentId { get; set; }

    public Guid ShipmentId { get; set; }

    [StringLength(100)]
    public string? GHNOrderCode { get; set; }

    [StringLength(100)]
    public string? ClientOrderCode { get; set; }

    [StringLength(100)]
    public string? GHNStatusCode { get; set; }

    public int? ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public int? FromDistrictId { get; set; }

    [StringLength(20)]
    public string? FromWardCode { get; set; }

    public int? ToDistrictId { get; set; }

    [StringLength(20)]
    public string? ToWardCode { get; set; }

    public int? Weight { get; set; }

    public int? Length { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? CODAmount { get; set; }

    public int PaymentTypeId { get; set; }

    public int InsuranceValue { get; set; }

    public int? RequiredNote { get; set; }

    [Precision(18, 2)]
    public decimal? GHNServiceFee { get; set; }

    [Precision(18, 2)]
    public decimal? GHNCodFee { get; set; }

    [Precision(18, 2)]
    public decimal? GHNTotalFee { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    [ForeignKey("ShipmentId")]
    [InverseProperty("GHN_Shipment")]
    public virtual Shipment Shipment { get; set; } = null!;
}
