using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class AuditLogMapper
    {
        public static audit_log ToDomain(this Audit_Log entity)
        {
            return new audit_log
            {
                AuditId = entity.AuditId,
                UserId = entity.UserId,
                UserRole = entity.UserRole,
                Action = entity.Action,
                OldValue = entity.OldValue,
                NewValue = entity.NewValue,
                TargetTable = entity.TargetTable,
                TargetId = entity.TargetId,
                CreatedAt = entity.CreatedAt
            };
        }

        public static Audit_Log ToInfrastructure(this audit_log entity)
        {
            return new Audit_Log
            {
                AuditId = entity.AuditId,
                UserId = entity.UserId,
                UserRole = entity.UserRole,
                Action = entity.Action,
                OldValue = entity.OldValue,
                NewValue = entity.NewValue,
                TargetTable = entity.TargetTable,
                TargetId = entity.TargetId,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}