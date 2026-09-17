using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Audits;
using HomeCycle.Application.DTOs.Responses.Audits;
using HomeCycle.Application.Interfaces.Repositories.Audits;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Audits
{
    public sealed class AuditLogRepository : IAuditLogRepository
    {
        private readonly HomeCycleDbContext _db;

        public AuditLogRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<AuditLogListItemResponse>> GetPagedAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = ApplyFilters(_db.Audit_Logs.AsNoTracking(), request);
            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.RecordedAtUtc)
                .ThenByDescending(x => x.AuditId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new AuditLogListItemResponse
                {
                    AuditId = x.AuditId,
                    EventId = x.EventId,
                    Category = (AuditCategory)x.Category,
                    Action = x.Action,
                    Outcome = (AuditOutcome)x.Outcome,
                    ReasonCode = x.ReasonCode,
                    ActorType = (AuditActorType)x.ActorType,
                    UserId = x.UserId,
                    UserRole = x.UserRole.HasValue ? (UserRole?)x.UserRole.Value : null,
                    TargetType = x.TargetType,
                    TargetId = x.TargetId,
                    Source = (AuditSource)x.Source,
                    CorrelationId = x.CorrelationId,
                    OccurredAtUtc = x.OccurredAtUtc,
                    RecordedAtUtc = x.RecordedAtUtc
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLogListItemResponse>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<audit_log?> GetByIdAsync(Guid auditId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Audit_Logs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AuditId == auditId, cancellationToken);

            return entity?.ToDomain();
        }

        private static IQueryable<Audit_Log> ApplyFilters(IQueryable<Audit_Log> query, AuditLogSearchRequest request)
        {
            if (request.FromUtc.HasValue)
                query = query.Where(x => x.OccurredAtUtc >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.OccurredAtUtc <= request.ToUtc.Value);

            if (request.Category.HasValue)
                query = query.Where(x => x.Category == (int)request.Category.Value);

            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                var action = request.Action.Trim();
                query = query.Where(x => x.Action == action);
            }

            if (request.Outcome.HasValue)
                query = query.Where(x => x.Outcome == (int)request.Outcome.Value);

            if (request.ActorType.HasValue)
                query = query.Where(x => x.ActorType == (int)request.ActorType.Value);

            if (request.UserId.HasValue)
                query = query.Where(x => x.UserId == request.UserId.Value);

            if (request.UserRole.HasValue)
                query = query.Where(x => x.UserRole == (int)request.UserRole.Value);

            if (!string.IsNullOrWhiteSpace(request.TargetType))
            {
                var targetType = request.TargetType.Trim();
                query = query.Where(x => x.TargetType == targetType);
            }

            if (request.TargetId.HasValue)
                query = query.Where(x => x.TargetId == request.TargetId.Value);

            if (request.Source.HasValue)
                query = query.Where(x => x.Source == (int)request.Source.Value);

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                var correlationId = request.CorrelationId.Trim();
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            return query;
        }
    }
}
