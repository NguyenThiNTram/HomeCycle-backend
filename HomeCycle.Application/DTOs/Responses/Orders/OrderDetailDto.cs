using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Orders
{
    public class OrderDetailDto
    {
        public order Order { get; set; } = null!;
        public string? PostDescription { get; set; }
        public review? Review { get; set; }
        public IReadOnlyList<shipment> Shipments { get; set; } = new List<shipment>();
        public IReadOnlyList<dispute> Disputes { get; set; } = new List<dispute>();
    }
}
