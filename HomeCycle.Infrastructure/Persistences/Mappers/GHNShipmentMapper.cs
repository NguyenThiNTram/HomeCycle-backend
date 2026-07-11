using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class GHNShipmentMapper
    {
        public static ghn_shipment ToDomain(this GHN_Shipment entity)
        {
            if (entity == null) return null;
            return new ghn_shipment
            {
                GHNShipmentId = entity.GHNShipmentId,
                ShipmentId = entity.ShipmentId,
                GHNOrderCode = entity.GHNOrderCode,
                ClientOrderCode = entity.ClientOrderCode,
                GHNStatusCode = entity.GHNStatusCode,
                ServiceId = entity.ServiceId,
                ServiceTypeId = entity.ServiceTypeId,
                FromDistrictId = entity.FromDistrictId,
                FromWardCode = entity.FromWardCode,
                ToDistrictId = entity.ToDistrictId,
                ToWardCode = entity.ToWardCode,
                Weight = entity.Weight,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                CODAmount = entity.CODAmount,
                PaymentTypeId = entity.PaymentTypeId,
                InsuranceValue = entity.InsuranceValue,
                RequiredNote = entity.RequiredNote,
                GHNServiceFee = entity.GHNServiceFee,
                GHNCodFee = entity.GHNCodFee,
                GHNTotalFee = entity.GHNTotalFee,
                CreatedAt = entity.CreatedAt,
                LastSyncedAt = entity.LastSyncedAt
            };
        }
        public static GHN_Shipment ToInfrastructure(this HomeCycle.Domain.Entities.ghn_shipment entity)
        {
            if (entity == null) return null;
            return new GHN_Shipment
            {
                GHNShipmentId = entity.GHNShipmentId,
                ShipmentId = entity.ShipmentId,
                GHNOrderCode = entity.GHNOrderCode,
                ClientOrderCode = entity.ClientOrderCode,
                GHNStatusCode = entity.GHNStatusCode,
                ServiceId = entity.ServiceId,
                ServiceTypeId = entity.ServiceTypeId,
                FromDistrictId = entity.FromDistrictId,
                FromWardCode = entity.FromWardCode,
                ToDistrictId = entity.ToDistrictId,
                ToWardCode = entity.ToWardCode,
                Weight = entity.Weight,
                Length = entity.Length,
                Width = entity.Width,
                Height = entity.Height,
                CODAmount = entity.CODAmount,
                PaymentTypeId = entity.PaymentTypeId,
                InsuranceValue = entity.InsuranceValue,
                RequiredNote = entity.RequiredNote,
                GHNServiceFee = entity.GHNServiceFee,
                GHNCodFee = entity.GHNCodFee,
                GHNTotalFee = entity.GHNTotalFee,
                CreatedAt = entity.CreatedAt,
                LastSyncedAt = entity.LastSyncedAt
            };
        }
    }
}
