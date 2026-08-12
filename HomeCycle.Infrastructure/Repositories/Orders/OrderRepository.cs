using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Orders
{
    public class OrderRepository : IOrderRepository
    {
        private readonly HomeCycleDbContext _db;
        public OrderRepository(HomeCycleDbContext db) => _db = db;

        public async Task<order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(order order, CancellationToken ct = default)
        {
            await _db.Orders.AddAsync(order.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(order order, CancellationToken ct = default)
        {
            _db.Orders.Update(order.ToInfrastructure());
            return Task.CompletedTask;
        }

        public async Task<order?> GetByAgreementIdAsync(Guid agreementId, CancellationToken ct = default)
        {
            var entity = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.AgreementId == agreementId, ct);
            return entity?.ToDomain();
        }

        public async Task<PagedResult<OrderListItemDto>> GetPagedByUserAsync(
            Guid userId,
            bool isSeller,
            OrderSearchRequest request,
            CancellationToken ct = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => isSeller ? o.Agreement.SellerId == userId : o.Agreement.BuyerId == userId);

            if (request.Status.HasValue)
                query = query.Where(o => o.OrderStatus == (int)request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(o => o.ProductName != null && o.ProductName.Contains(request.Keyword));

            query = query.OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderListItemDto
                {
                    OrderId = o.OrderId,
                    ProductName = o.ProductName,
                    // Lấy 1 ảnh đại diện của Post (DisplayOrder nhỏ nhất) làm thumbnail.
                    ThumbnailUrl = _db.Media
                        .Where(m => m.TargetId == o.PostId && m.TargetType == "Post")
                        .OrderBy(m => m.DisplayOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault(),
                    Quantity = o.Quantity,
                    FinalTotalAmount = o.FinalTotalAmount,
                    AmountPaid = o.AmountPaid,
                    AmountRemaining = o.AmountRemaining,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(ct);

            return new PagedResult<OrderListItemDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        public async Task<OrderDetailDto?> GetDetailWithRelationsAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Post)
                .Include(o => o.Reviews)
                .Include(o => o.Shipments)
                .Include(o => o.Disputes)
                .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

            if (entity == null)
                return null;

            return new OrderDetailDto
            {
                Order = entity.ToDomain(),
                PostDescription = entity.Post?.Description,
                Reviews = entity.Reviews.Select(r => r.ToDomain()).ToList(),
                Shipments = entity.Shipments.Select(s => s.ToDomain()).ToList(),
                Disputes = entity.Disputes.Select(d => d.ToDomain()).ToList()
            };
        }

    }
}
