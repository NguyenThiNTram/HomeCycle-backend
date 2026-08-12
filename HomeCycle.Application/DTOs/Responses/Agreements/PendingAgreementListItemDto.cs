using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Agreements
{
    public class PendingAgreementListItemDto
    {
        public Guid AgreementId { get; set; }
        public string? ProductName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int Quantity { get; set; }
        public decimal? FinalPrice { get; set; }
        public decimal? InitialPrice { get; set; }
        public string? SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
