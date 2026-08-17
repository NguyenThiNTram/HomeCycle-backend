using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Repositories.Disputes;
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

namespace HomeCycle.Infrastructure.Repositories.Disputes
{
    public class DisputeRepository : IDisputeRepository
    {
        private readonly HomeCycleDbContext _db;

        public DisputeRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(
            dispute dispute,
            CancellationToken cancellationToken = default)
        {
            await _db.Disputes.AddAsync(
                dispute.ToInfrastructure(),
                cancellationToken);
        }

        public async Task<dispute?> GetByIdAsync(
            Guid disputeId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Disputes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.DisputeId == disputeId,
                    cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsActiveAsync(
            DisputeTargetType targetType,
            Guid targetId,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Disputes
                .AsNoTracking()
                .Where(x =>
                    x.DisputeStatus == (int)DisputeStatus.Pending);

            query = targetType switch
            {
                DisputeTargetType.Order =>
                    query.Where(x => x.OrderId == targetId),
                DisputeTargetType.Review =>
                    query.Where(x => x.ReviewId == targetId),

                _ =>
                    query.Where(x => false)
            };

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<PagedResult<DisputeListItemResponse>>
            GetPagedForModeratorAsync(
                DisputeSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var query = _db.Disputes
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.TargetUser)
                .Include(x => x.Order)
                .AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.DisputeStatus ==
                    (int)request.Status.Value);
            }

            if (request.Category.HasValue)
            {
                query = query.Where(x =>
                    x.DisputeCategory ==
                    (int)request.Category.Value);
            }

            if (request.TargetType.HasValue)
            {
                query = query.Where(x =>
                    x.DisputeTargetType ==
                    (int)request.TargetType.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt <= request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                var pattern = $"%{keyword}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.Sender.Username,
                        pattern)
                    ||
                    (
                        x.TargetUser != null &&
                        EF.Functions.ILike(
                            x.TargetUser.Username,
                            pattern)
                    )
                    ||
                    (
                        x.Description != null &&
                        EF.Functions.ILike(
                            x.Description,
                            pattern)
                    )
                    ||
                    (
                        x.Order != null &&
                        EF.Functions.ILike(
                            x.Order.OrderCode,
                            pattern)
                    ));
            }

            query = query
                .OrderByDescending(x => x.CreatedAt);

            var totalCount =
                await query.CountAsync(cancellationToken);

            var entities = await query
                .Skip(
                    (request.PageNumber - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = entities
                .Select(x => new DisputeListItemResponse
                {
                    DisputeId = x.DisputeId,
                    SenderId = x.SenderId,
                    SenderUsername =
                        x.Sender.Username,
                    TargetUserId =
                        x.TargetUserId,
                    TargetUsername =
                        x.TargetUser?.Username,
                    TargetType =
                        x.DisputeTargetType.HasValue
                            ? (DisputeTargetType?)
                                x.DisputeTargetType.Value
                            : null,
                    TargetId =
                        x.OrderId ?? x.ReviewId,
                    OrderCode =
                        x.Order?.OrderCode,
                    Category =
                        x.DisputeCategory.HasValue
                            ? (DisputeCategory?)
                                x.DisputeCategory.Value
                            : null,
                    Status =
                        x.DisputeStatus.HasValue
                            ? (DisputeStatus?)
                                x.DisputeStatus.Value
                            : null,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return new PagedResult<DisputeListItemResponse>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
