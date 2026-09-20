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

                        SellerReadyAt = latestShipment.SellerReadyAt,
                        PickedUpAt = latestShipment.PickedUpAt,

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
                            : null,

                    LatestDisputeCreatedAt = latestDispute?.CreatedAt,
                    LatestDisputeResolvedAt = latestDispute?.ResolvedAt
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

                DeliveryMethod = latestShipment != null &&
                             latestShipment.DeliveryMethod != (int)DeliveryMethod.Unknown
                ? (DeliveryMethod?)latestShipment.DeliveryMethod
                : agreementDetails?.DeliveryMethod,

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

        public async Task<IReadOnlyList<Guid>> GetAutoCompleteCandidateIdsAsync(
            DateTime cutoffUtc,
            int limit,
            CancellationToken ct = default)
        {
            return await _db.Orders
                .AsNoTracking()
                .Where(o =>
                    o.OrderStatus == (int)OrderStatus.Processing &&
                    o.BuyerReceivedConfirmedAt == null &&
                    !o.Disputes.Any(d =>
                        d.DisputeStatus == (int)DisputeStatus.Pending ||
                        d.DisputeStatus == (int)DisputeStatus.UnderReview ||
                        d.DisputeStatus == (int)DisputeStatus.AwaitingReturn) &&
                    (
                        o.Shipments.Any(s =>
                            s.DeliveryMethod == (int)DeliveryMethod.GhnDelivery &&
                            s.ShipmentStatus == (int)ShipmentStatus.Delivered &&
                            s.DeliveredAt != null &&
                            s.DeliveredAt <= cutoffUtc)
                        ||
                        (
                            o.SellerHandoverConfirmedAt != null &&
                            o.SellerHandoverConfirmedAt <= cutoffUtc &&
                            o.Shipments.Any(s =>
                                s.DeliveryMethod == (int)DeliveryMethod.SellerDelivers ||
                                s.DeliveryMethod == (int)DeliveryMethod.BuyerPickUp)
                        )
                    ))
                .OrderBy(o => o.UpdatedAt)
                .Select(o => o.OrderId)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Guid>> GetAutoReleaseCandidateIdsAsync(
            DateTime nowUtc,
            int limit,
            CancellationToken ct = default)
        {
            return await _db.Orders
                .AsNoTracking()
                .Where(o =>
                    o.OrderStatus == (int)OrderStatus.Completed &&
                    o.DisputeWindowEndsAt.HasValue &&
                    o.DisputeWindowEndsAt.Value < nowUtc &&
                    !o.Disputes.Any(d =>
                        d.DisputeStatus == (int)DisputeStatus.Pending ||
                        d.DisputeStatus == (int)DisputeStatus.UnderReview ||
                        d.DisputeStatus == (int)DisputeStatus.AwaitingReturn) &&
                    !_db.Wallet_Transactions.Any(t =>
                        t.ReferenceType == (int)ReferenceType.Order &&
                        t.ReferenceId == o.OrderId &&
                        t.TransactionType == (int)TransactionType.Payout_Release &&
                        t.WalletTransactionStatus ==
                            (int)WalletTransactionStatus.Completed))
                .OrderBy(o => o.DisputeWindowEndsAt)
                .ThenBy(o => o.OrderId)
                .Select(o => o.OrderId)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<ModeratorOrderReadModel>> GetPagedForModeratorAsync(
            ModeratorOrderQuery request,
            CancellationToken ct = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .AsQueryable();

            if (request.DeliveryMethod.HasValue)
                query = query.Where(o => o.Shipments.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.ShipmentId)
                    .Select(s => (int?)s.DeliveryMethod).FirstOrDefault() == (int)request.DeliveryMethod.Value);

            var shipmentIssueStatuses = new int?[] { (int)ShipmentStatus.Cancelled, (int)ShipmentStatus.Exception,
                (int)ShipmentStatus.Damage_Lost, (int)ShipmentStatus.Returning, (int)ShipmentStatus.Returned };
            var issues = query.Where(o => o.OrderStatus == (int)OrderStatus.Cancelled || o.OrderStatus == (int)OrderStatus.Returned
                || o.OrderStatus == (int)OrderStatus.Disputing
                || o.Disputes.Any(d => d.DisputeStatus == (int)DisputeStatus.Pending || d.DisputeStatus == (int)DisputeStatus.UnderReview || d.DisputeStatus == (int)DisputeStatus.AwaitingReturn)
                || shipmentIssueStatuses.Contains(o.Shipments.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.ShipmentId)
                    .Select(s => s.ShipmentStatus).FirstOrDefault()));
            if (request.Group == OrderHistoryGroup.Issue) query = issues;
            else if (request.Group == OrderHistoryGroup.Successful)
                query = query.Where(o => o.OrderStatus == (int)OrderStatus.Completed && !issues.Any(i => i.OrderId == o.OrderId));
            else if (request.Group == OrderHistoryGroup.Trading)
                query = query.Where(o => (o.OrderStatus == (int)OrderStatus.Pending || o.OrderStatus == (int)OrderStatus.Processing)
                    && !issues.Any(i => i.OrderId == o.OrderId));

            if (request.Status.HasValue)
            {
                query = query.Where(o =>
                    o.OrderStatus == (int)request.Status.Value);
            }

            if (request.PaymentStatus.HasValue)
            {
                query = query.Where(o =>
                    o.PaymentStatus == (int)request.PaymentStatus.Value);
            }

            if (request.BuyerId.HasValue)
            {
                query = query.Where(o =>
                    o.Agreement.BuyerId == request.BuyerId.Value);
            }

            if (request.SellerId.HasValue)
            {
                query = query.Where(o =>
                    o.Agreement.SellerId == request.SellerId.Value);
            }

            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(o =>
                    o.CreatedAt >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                query = query.Where(o =>
                    o.CreatedAt <= request.CreatedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(o =>
                    EF.Functions.ILike(
                        o.OrderCode,
                        $"%{keyword}%") ||

                    (o.ProductName != null &&
                     EF.Functions.ILike(
                         o.ProductName,
                         $"%{keyword}%")) ||

                    EF.Functions.ILike(
                        o.Agreement.Buyer.Username,
                        $"%{keyword}%") ||

                    EF.Functions.ILike(
                        o.Agreement.Seller.Username,
                        $"%{keyword}%"));
            }

            if (request.HasActiveDispute.HasValue)
            {
                if (request.HasActiveDispute.Value)
                {
                    query = query.Where(o =>
                        o.Disputes.Any(d =>
                            d.DisputeStatus == (int)DisputeStatus.Pending ||
                            d.DisputeStatus == (int)DisputeStatus.UnderReview ||
                            d.DisputeStatus == (int)DisputeStatus.AwaitingReturn));
                }
                else
                {
                    query = query.Where(o =>
                        !o.Disputes.Any(d =>
                            d.DisputeStatus == (int)DisputeStatus.Pending ||
                            d.DisputeStatus == (int)DisputeStatus.UnderReview ||
                            d.DisputeStatus == (int)DisputeStatus.AwaitingReturn));
                }
            }

            if (request.HasInspection.HasValue)
            {
                if (request.HasInspection.Value)
                {
                    query = query.Where(o =>
                        _db.Inspection_Forms.Any(f =>
                            f.OrderId == o.OrderId));
                }
                else
                {
                    query = query.Where(o =>
                        !_db.Inspection_Forms.Any(f =>
                            f.OrderId == o.OrderId));
                }
            }

            var totalCount =
                await query.CountAsync(ct);

            var page = query
                .OrderByDescending(o => o.CreatedAt)
                .ThenBy(o => o.OrderId)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            var items = await ProjectModeratorOrders(page)
                .ToListAsync(ct);

            return new PagedResult<ModeratorOrderReadModel>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task<ModeratorOrderReadModel?> GetForModeratorAsync(
            Guid orderId,
            CancellationToken ct = default)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == orderId);

            return ProjectModeratorOrders(query)
                .FirstOrDefaultAsync(ct);
        }

        private IQueryable<ModeratorOrderReadModel> ProjectModeratorOrders(
            IQueryable<Order> query)
        {
            return query.Select(o =>
                new ModeratorOrderReadModel
                {
                    LastPaidAt = _db.Payments.Where(p => (p.OrderId == o.OrderId || p.AgreementId == o.AgreementId)
                            && (p.PaymentType == (int)PaymentType.Deposit || p.PaymentType == (int)PaymentType.Full_Payment)
                            && (p.PaymentStatus == (int)PaymentStatus.Completed || p.PaymentStatus == (int)PaymentStatus.Refunded
                                || p.PaymentStatus == (int)PaymentStatus.PartiallyRefunded))
                        .Max(p => p.PaidAt),
                    DeliveryMethod = o.Shipments.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.ShipmentId)
                        .Select(s => (int?)s.DeliveryMethod).FirstOrDefault(),
                    ShipmentStatus = o.Shipments.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.ShipmentId)
                        .Select(s => s.ShipmentStatus).FirstOrDefault(),
                    CollectionDate = o.Agreement.Appointments.Where(a => a.AppointmentType == (int)AppointmentType.Collection
                            && a.AppointmentStatus != (int)AppointmentStatus.Proposed && a.CancellationReason != "Rescheduled")
                        .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.AppointmentId)
                        .Select(a => a.Collection_Appointment!.CollectionDate).FirstOrDefault(),
                    OrderId = o.OrderId,
                    AgreementId = o.AgreementId,
                    PostId = o.PostId,

                    OrderCode = o.OrderCode,
                    ProductName = o.ProductName,

                    ThumbnailUrl = _db.Media
                        .Where(m =>
                            m.TargetId == o.PostId &&
                            m.TargetType == "Post")
                        .OrderBy(m => m.DisplayOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault(),

                    Quantity = o.Quantity,

                    FinalTotalAmount = o.FinalTotalAmount,
                    AmountPaid = o.AmountPaid,
                    AmountRemaining = o.AmountRemaining,

                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,

                    BuyerId = o.Agreement.BuyerId,
                    BuyerUsername = o.Agreement.Buyer.Username,
                    BuyerPhoneNumber = o.Agreement.Buyer.PhoneNumber,
                    BuyerAvatarUrl = o.Agreement.Buyer.AvatarUrl,

                    SellerId = o.Agreement.SellerId,
                    SellerUsername = o.Agreement.Seller.Username,
                    SellerPhoneNumber = o.Agreement.Seller.PhoneNumber,
                    SellerAvatarUrl = o.Agreement.Seller.AvatarUrl,

                    HasActiveDispute =
                        o.Disputes.Any(d =>
                            d.DisputeStatus == (int)DisputeStatus.Pending ||
                            d.DisputeStatus == (int)DisputeStatus.UnderReview ||
                            d.DisputeStatus == (int)DisputeStatus.AwaitingReturn),

                    LatestDisputeId = o.Disputes
                        .OrderByDescending(d => d.CreatedAt)
                        .Select(d => (Guid?)d.DisputeId)
                        .FirstOrDefault(),

                    LatestDisputeStatus = o.Disputes
                        .OrderByDescending(d => d.CreatedAt)
                        .Select(d => d.DisputeStatus)
                        .FirstOrDefault(),

                    HasInspection =
                        _db.Inspection_Forms.Any(f =>
                            f.OrderId == o.OrderId),

                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                });
        }
    }
}
