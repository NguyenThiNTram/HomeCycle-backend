using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class InspectionFormMapper
    {
        public static inspection_form ToDomain(this Inspection_Form entity)
        {
            return new inspection_form
            {
                InspectionFormId = entity.InspectionFormId,
                InspectionAppointmentId = entity.InspectionAppointmentId,
                OrderId = entity.OrderId,
                InspectorId = entity.InspectorId,
                InspectionTime = entity.InspectionTime,
                OperatingStatus = entity.OperatingStatus,
                AppearanceStatus = entity.AppearanceStatus,
                PartsStatus = entity.PartsStatus,
                MatchStatus = entity.MatchStatus,
                InspectorNotes = entity.InspectorNotes,
                Conclusion = entity.Conclusion,
                OriginalPrice = entity.OriginalPrice,
                SuggestedPrice = entity.SuggestedPrice,
                CollectAction = entity.CollectAction,
                InspectionStatus = entity.InspectionStatus,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Inspection_Form ToInfrastructure(this inspection_form entity)
        {
            return new Inspection_Form
            {
                InspectionFormId = entity.InspectionFormId,
                InspectionAppointmentId = entity.InspectionAppointmentId,
                OrderId = entity.OrderId,
                InspectorId = entity.InspectorId,
                InspectionTime = entity.InspectionTime,
                OperatingStatus = entity.OperatingStatus,
                AppearanceStatus = entity.AppearanceStatus,
                PartsStatus = entity.PartsStatus,
                MatchStatus = entity.MatchStatus,
                InspectorNotes = entity.InspectorNotes,
                Conclusion = entity.Conclusion,
                OriginalPrice = entity.OriginalPrice,
                SuggestedPrice = entity.SuggestedPrice,
                CollectAction = entity.CollectAction,
                InspectionStatus = entity.InspectionStatus,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
