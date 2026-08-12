using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.Interfaces.Repositories.Orders;
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
                    OrderCode = o.OrderCode,
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
        public async Task<OrderDetailDto?> GetDetailWithRelationsAsync(Guid orderId, Guid currentUserId, CancellationToken ct = default)
        {
            var entity = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Post)
                .Include(o => o.Review)
                .Include(o => o.Shipments)
                .Include(o => o.Disputes)
                .Include(o => o.Payments)
                .Include(o => o.Agreement).ThenInclude(a => a.Buyer)
                .Include(o => o.Agreement).ThenInclude(a => a.Seller)
                .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

            if (entity == null)
                return null;

            bool isBuyer = entity.Agreement.BuyerId == currentUserId;


            var thumbnailUrl = await _db.Media
                .Where(m => m.TargetId == entity.PostId && m.TargetType == "Post")
                .OrderBy(m => m.DisplayOrder)
                .Select(m => m.Url)
                .FirstOrDefaultAsync(ct);

            // 1 Order có thể có nhiều Payment (cọc, thanh toán phần còn lại...).
            // Lấy Payment MỚI NHẤT đã thanh toán thành công để hiển thị PaymentMethod/PaidAt cho detail.
            // PaymentStatus.Completed/Success thật của bạn.
            var latestPaidPayment = entity.Payments
                .Where(p => p.PaymentStatus == 1 && p.PaidAt.HasValue)
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefault();

            // Review: giả định chỉ Buyer được đánh giá Seller sau khi đơn Completed và chưa từng review.
            // TODO: đổi "2" thành đúng giá trị enum OrderStatus.Completed thật của bạn.
            var reviewSummary = new ReviewSummaryDto
            {
                HasReviewed = entity.Review != null,
                CanReview = isBuyer && entity.OrderStatus == 2 && entity.Review == null,
                Rating = entity.Review?.Rating
            };

            // Giả định 1 Order chỉ theo dõi 1 Shipment chính (lấy bản ghi mới nhất nếu có nhiều).
            var latestShipment = entity.Shipments
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            var shipmentSummary = latestShipment == null ? null : new ShipmentSummaryDto
            {
                ShipmentId = latestShipment.ShipmentId,
                ShipmentStatus = latestShipment.ShipmentStatus,
                DeliveredAt = latestShipment.DeliveredAt
            };


            // Dispute được coi là "active" khi còn Pending hoặc đang UnderReview —
            // Resolved/Rejected/Closed không còn ảnh hưởng tới đơn hàng nữa.
            var latestDispute = entity.Disputes
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
            var disputeSummary = new DisputeSummaryDto
            {
                HasActiveDispute = latestDispute != null
                    && (latestDispute.DisputeStatus == (int)DisputeStatus.Pending),
                LatestDisputeId = latestDispute?.DisputeId
            };

            return new OrderDetailDto
            {
                Order = entity.ToDomain(),
                ThumbnailUrl = thumbnailUrl,
                PostDescription = entity.Post?.Description,
                CounterpartyName = isBuyer ? entity.Agreement.Seller.Username : entity.Agreement.Buyer.Username,
                NegotiationId = entity.Agreement.NegotiationId,
                PaymentMethod = latestPaidPayment?.PaymentMethod,
                PaidAt = latestPaidPayment?.PaidAt,
                Review = reviewSummary,
                Shipment = shipmentSummary,
                Dispute = disputeSummary
            };
        }

    }
}
