using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class AppointmentMapper
    {
        public static appointment ToDomain(this Appointment entity)
        {
            return new appointment
            {
                AppointmentId = entity.AppointmentId,
                AgreementId = entity.AgreementId,
                AppointmentType = entity.AppointmentType,
                AppointmentStatus = entity.AppointmentStatus,
                BuyerCheckAt = entity.BuyerCheckAt,
                SellerCheckAt = entity.SellerCheckAt,
                CreatedAt = entity.CreatedAt,
                CancelledAt = entity.CancelledAt,
                CancellationReason = entity.CancellationReason,
                CompletedAt = entity.CompletedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static Appointment ToInfrastructure(appointment entity)
        {
            return new Appointment
            {
                AppointmentId = entity.AppointmentId,
                AgreementId = entity.AgreementId,
                AppointmentType = entity.AppointmentType,
                AppointmentStatus = entity.AppointmentStatus,
                BuyerCheckAt = entity.BuyerCheckAt,
                SellerCheckAt = entity.SellerCheckAt,
                CreatedAt = entity.CreatedAt,
                CancelledAt = entity.CancelledAt,
                CancellationReason = entity.CancellationReason,
                CompletedAt = entity.CompletedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
