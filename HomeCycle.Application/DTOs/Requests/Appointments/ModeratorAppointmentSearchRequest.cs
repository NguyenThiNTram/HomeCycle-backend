using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Appointments
{
    public class ModeratorAppointmentSearchRequest : PaginationRequest
    {
        public string? Keyword { get; set; }
        public AppointmentType? Type { get; set; }
        public AppointmentStatus? Status { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? BuyerId { get; set; }
        public Guid? SellerId { get; set; }
        public bool? IsOverdue { get; set; }
        public bool? HasInspectionForm { get; set; }
        public DateTime? ScheduledFrom { get; set; }
        public DateTime? ScheduledTo { get; set; }
    }

    public sealed class ModeratorAppointmentQuery
    {
        public string? Keyword { get; init; }
        public AppointmentType? Type { get; init; }
        public AppointmentStatus? Status { get; init; }
        public Guid? OrderId { get; init; }
        public Guid? BuyerId { get; init; }
        public Guid? SellerId { get; init; }
        public bool? IsOverdue { get; init; }
        public bool? HasInspectionForm { get; init; }
        public DateTime? ScheduledFrom { get; init; }
        public DateTime? ScheduledTo { get; init; }
        public DateTime NowUtc { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
    }
}
