using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Shipment")]
[Index("OrderId", Name = "idx_shipment_order")]
public partial class Shipment
{
    [Key]
    public Guid ShipmentId { get; set; }

    public Guid OrderId { get; set; }

    public Guid? CollectionAppointmentId { get; set; }

    public int? DeliveryMethod { get; set; }

    public int? ShipmentStatus { get; set; }

    [StringLength(255)]
    public string? FromName { get; set; }

    [StringLength(20)]
    public string? FromPhone { get; set; }

    public string? PickupAddress { get; set; }

    public DateTime? PickedUpAt { get; set; }

    [StringLength(255)]
    public string? ToName { get; set; }

    [StringLength(20)]
    public string? ToPhone { get; set; }

    public string? DeliveryAddress { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CollectionAppointmentId")]
    [InverseProperty("Shipments")]
    public virtual Collection_Appointment? CollectionAppointment { get; set; }

    [InverseProperty("Shipment")]
    public virtual GHN_Shipment? GHN_Shipment { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Shipments")]
    public virtual Order Order { get; set; } = null!;
}
