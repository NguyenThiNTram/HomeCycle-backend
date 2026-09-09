using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Shipments
{
    public class ShipmentSellerReadyResponseDto
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public DateTime? SellerReadyAt { get; set; }
    }
}
