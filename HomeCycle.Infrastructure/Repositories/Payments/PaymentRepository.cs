using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Payments;
using HomeCycle.Application.DTOs.Responses.Payments;
using HomeCycle.Application.Interfaces.Repositories.Payments;
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

namespace HomeCycle.Infrastructure.Repositories.Payments
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly HomeCycleDbContext _db;
        public PaymentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default)
        {
            var entity = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);
            return entity?.ToDomain();
        }

        public async Task AddAsync(payment payment, CancellationToken ct = default)
        {
            await _db.Payments.AddAsync(payment.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(payment payment, CancellationToken ct = default)
        {
            _db.Payments.Update(payment.ToInfrastructure());
            return Task.CompletedTask;
        }

        public async Task<payment?> GetLatestPendingByAgreementAsync(Guid agreementId, CancellationToken ct = default)
        {
            var entity = await _db.Payments
                .AsNoTracking()
                .Where(x => x.AgreementId == agreementId && x.PaymentStatus == (int)PaymentStatus.Pending)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            return entity?.ToDomain();
        }

        public async Task<PagedResult<PaymentHistoryResponseDto>> GetPagedPaymentHistoryAsync(Guid userId, PaymentHistorySearchRequest request, CancellationToken ct = default)
        {
            var query = _db.Payments
                .AsNoTracking()
                .Where(x => x.PayerId == userId);

            if (request.Status.HasValue)
                query = query.Where(x => x.PaymentStatus == (int)request.Status.Value);

            if (request.Method.HasValue)
                query = query.Where(x => x.PaymentMethod == (int)request.Method.Value);

            if (request.FromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= request.FromDate.Value.ToUniversalTime());

            if (request.ToDate.HasValue)
                query = query.Where(x => x.CreatedAt <= request.ToDate.Value.ToUniversalTime());

            var totalCount = await query.CountAsync(ct);

            // Kỹ thuật Projection: Chỉ Select đúng cột cần thiết, không kéo nguyên bảng Payment về RAM
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new PaymentHistoryResponseDto
                {
                    PaymentId = x.PaymentId,
                    CreatedAt = x.CreatedAt,
                    Description = x.Description ?? string.Empty,
                    Amount = x.Amount ?? 0,
                    PaymentMethod = x.PaymentMethod.HasValue ? (PaymentMethod)x.PaymentMethod.Value : PaymentMethod.Unknown,
                    PaymentStatus = x.PaymentStatus.HasValue ? (PaymentStatus)x.PaymentStatus.Value : PaymentStatus.Pending,
                    OrderId = x.OrderId
                })
                .ToListAsync(ct);

            return new PagedResult<PaymentHistoryResponseDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<payment?> GetLatestPaidByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        {
            var entity = await _db.Payments
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == orderId &&
                    x.PaidAt.HasValue &&
                    (
                        x.PaymentStatus == (int)PaymentStatus.Completed ||
                        x.PaymentStatus == (int)PaymentStatus.PartiallyRefunded ||
                        x.PaymentStatus == (int)PaymentStatus.Refunded
                    ))
                .OrderByDescending(x => x.PaidAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }
    }
}
