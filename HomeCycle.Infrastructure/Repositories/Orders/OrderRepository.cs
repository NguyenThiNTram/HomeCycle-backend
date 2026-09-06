using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Agreements;
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
using System.Text.Json;
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
        public async Task<OrderDetailDto?> GetDetailWithRelationsAsync(
            Guid orderId,
            Guid currentUserId,
            CancellationToken ct = default)
        {
            var entity = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Post)
                .Include(o => o.Reviews)
                .Include(o => o.Shipments)
                .Include(o => o.Disputes)
                .Include(o => o.Payments)
                .Include(o => o.Agreement)
                    .ThenInclude(a => a.Buyer)
                .Include(o => o.Agreement)
                    .ThenInclude(a => a.Seller)
                .FirstOrDefaultAsync(
                    x => x.OrderId == orderId,
                    ct);

            if (entity == null)
                return null;

            var isBuyer =
                entity.Agreement.BuyerId ==
                currentUserId;

            var counterparty =
                isBuyer
                    ? entity.Agreement.Seller
                    : entity.Agreement.Buyer;

            var thumbnailUrl =
                await _db.Media
                    .Where(m =>
                        m.TargetId == entity.PostId &&
                        m.TargetType == "Post")
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => m.Url)
                    .FirstOrDefaultAsync(ct);

            var latestPaidPayment =
                entity.Payments
                    .Where(p =>
                        p.PaidAt.HasValue &&
                        (
                            p.PaymentStatus ==
                                (int)PaymentStatus.Completed ||
                            p.PaymentStatus ==
                                (int)PaymentStatus.PartiallyRefunded ||
                            p.PaymentStatus ==
                                (int)PaymentStatus.Refunded
                        ))
                    .OrderByDescending(p => p.PaidAt)
                    .FirstOrDefault();

            var latestShipment =
                entity.Shipments
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault();

            var latestDispute =
                entity.Disputes
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefault();

            AgreementDetailsDto? agreementDetails = null;

            if (!string.IsNullOrWhiteSpace(
                entity.Agreement.AgreementDetailsJsonb))
            {
                try
                {
                    agreementDetails =
                        JsonSerializer.Deserialize<AgreementDetailsDto>(
                            entity.Agreement.AgreementDetailsJsonb,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch (JsonException)
                {
                    agreementDetails = null;
                }
            }

            var counterpartySummary =
                new CounterpartySummaryDto
                {
                    UserId = counterparty.UserId,
                    Username = counterparty.Username,
                    PhoneNumber = counterparty.PhoneNumber,
                    AvatarUrl = counterparty.AvatarUrl
                };

            PaymentSummaryDto? paymentSummary = null;

            if (latestPaidPayment != null)
            {
                paymentSummary =
                    new PaymentSummaryDto
                    {
                        PaymentId =
                            latestPaidPayment.PaymentId,

                        PaymentMethod =
                            latestPaidPayment.PaymentMethod.HasValue
                                ? (PaymentMethod?)
                                    latestPaidPayment.PaymentMethod.Value
                                : null,

                        PaymentStatus =
                            latestPaidPayment.PaymentStatus.HasValue
                                ? (PaymentStatus?)
                                    latestPaidPayment.PaymentStatus.Value
                                : null,

                        Amount =
                            latestPaidPayment.Amount,

                        PaidAt =
                            latestPaidPayment.PaidAt
                    };
            }

            ShipmentSummaryDto? shipmentSummary = null;

            if (latestShipment != null)
            {
                shipmentSummary =
                    new ShipmentSummaryDto
                    {
                        ShipmentId =
                            latestShipment.ShipmentId,

                        ShipmentStatus =
                            latestShipment.ShipmentStatus.HasValue
                                ? (ShipmentStatus?)
                                    latestShipment.ShipmentStatus.Value
                                : null,

                        DeliveredAt =
                            latestShipment.DeliveredAt
                    };
            }

            var disputeSummary =
                new DisputeSummaryDto
                {
                    HasActiveDispute =
                        latestDispute?.DisputeStatus == (int)DisputeStatus.Pending ||
                        latestDispute?.DisputeStatus == (int)DisputeStatus.UnderReview ||
                        latestDispute?.DisputeStatus == (int)DisputeStatus.AwaitingReturn,

                    LatestDisputeId =
                        latestDispute?.DisputeId,

                    LatestDisputeStatus =
                        latestDispute?.DisputeStatus.HasValue ==
                        true
                            ? (DisputeStatus?)
                                latestDispute.DisputeStatus.Value
                            : null
                };

            var reviews =
                entity.Reviews
                    .Select(r => r.ToDomain())
                    .ToList();

            OrderCancellationDto? cancellation = null;

            if (entity.CancelledAt.HasValue)
            {
                cancellation =
                    new OrderCancellationDto
                    {
                        CancelledAt =
                            entity.CancelledAt.Value,

                        CancelledByUserId =
                            entity.CancelledByUserId,

                        Reason =
                            entity.CancellationReason
                    };
            }

            return new OrderDetailDto
            {
                OrderId = entity.OrderId,
                AgreementId = entity.AgreementId,
                PostId = entity.PostId,
                NegotiationId =
                    entity.Agreement.NegotiationId,

                OrderCode = entity.OrderCode,
                ProductName = entity.ProductName,
                Quantity = entity.Quantity,

                OriginalTotalAmount =
                    entity.OriginalTotalAmount,

                FinalTotalAmount =
                    entity.FinalTotalAmount,

                AmountPaid =
                    entity.AmountPaid,

                AmountRemaining =
                    entity.AmountRemaining,

                ShippingFee =
                    agreementDetails?.EstimatedShippingFee,

                PaymentStatus =
                    entity.PaymentStatus.HasValue
                        ? (PaymentStatus?)
                            entity.PaymentStatus.Value
                        : null,

                OrderStatus =
                    entity.OrderStatus.HasValue
                        ? (OrderStatus?)
                            entity.OrderStatus.Value
                        : null,

                DeliveryMethod =
                    agreementDetails?.DeliveryMethod,

                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CompletedAt = entity.CompletedAt,

                BuyerReturnConfirmedAt = entity.BuyerReturnConfirmedAt,
                SellerReturnReceivedAt = entity.SellerReturnReceivedAt,
                ReturnDueAt = entity.ReturnDueAt,
                ReturnedAt = entity.ReturnedAt,

                SellerHandoverConfirmedAt =
                    entity.SellerHandoverConfirmedAt,

                BuyerReceivedConfirmedAt =
                    entity.BuyerReceivedConfirmedAt,

                CompletionSource =
                    entity.CompletionSource.HasValue
                        ? (OrderCompletionSource?)
                            entity.CompletionSource.Value
                        : null,

                DisputeWindowEndsAt =
                    entity.DisputeWindowEndsAt,

                Cancellation =
                    cancellation,

                ThumbnailUrl =
                    thumbnailUrl,

                PostDescription =
                    entity.Post?.Description,

                Counterparty =
                    counterpartySummary,

                Payment =
                    paymentSummary,

                Shipment =
                    shipmentSummary,

                Reviews =
                    reviews,

                Dispute =
                    disputeSummary
            };
        }

        public async Task<order?> GetByIdForUpdateAsync(
            Guid orderId,
            CancellationToken ct = default)
        {
            var entity = await _db.Orders
                .FromSqlInterpolated($"SELECT * FROM public.\"Order\" WHERE \"OrderId\" = {orderId} FOR UPDATE")
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }
    }
}
