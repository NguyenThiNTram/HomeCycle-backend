using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
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
                EventId = entity.EventId,
                Category = (AuditCategory)entity.Category,
                Action = entity.Action,
                Outcome = (AuditOutcome)entity.Outcome,
                ReasonCode = entity.ReasonCode,
                ActorType = (AuditActorType)entity.ActorType,
                UserId = entity.UserId,
                UserRole = entity.UserRole.HasValue ? (UserRole?)entity.UserRole.Value : null,
                TargetType = entity.TargetType,
                TargetId = entity.TargetId,
                OldValues = entity.OldValues,
                NewValues = entity.NewValues,
                Source = (AuditSource)entity.Source,
                CorrelationId = entity.CorrelationId,
                IpAddress = entity.IpAddress,
                UserAgent = entity.UserAgent,
                OccurredAtUtc = entity.OccurredAtUtc,
                RecordedAtUtc = entity.RecordedAtUtc,
                Metadata = entity.Metadata
            };
        }

        public static Audit_Log ToInfrastructure(this audit_log entity)
        {
            return new Audit_Log
            {
                AuditId = entity.AuditId,
                EventId = entity.EventId,
                Category = (int)entity.Category,
                Action = entity.Action,
                Outcome = (int)entity.Outcome,
                ReasonCode = entity.ReasonCode,
                ActorType = (int)entity.ActorType,
                UserId = entity.UserId,
                UserRole = entity.UserRole.HasValue ? (int?)entity.UserRole.Value : null,
                TargetType = entity.TargetType,
                TargetId = entity.TargetId,
                OldValues = entity.OldValues,
                NewValues = entity.NewValues,
                Source = (int)entity.Source,
                CorrelationId = entity.CorrelationId,
                IpAddress = entity.IpAddress,
                UserAgent = entity.UserAgent,
                OccurredAtUtc = entity.OccurredAtUtc,
                RecordedAtUtc = entity.RecordedAtUtc,
                Metadata = entity.Metadata
            };
        }
    }
}