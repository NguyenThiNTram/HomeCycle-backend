using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Agreements;
using HomeCycle.Application.DTOs.Responses.Agreements;
using HomeCycle.Application.Interfaces.Repositories.Agreements;
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

namespace HomeCycle.Infrastructure.Repositories.Agreements
{
    public class AgreementFormRepository : IAgreementFormRepository
    {
        private readonly HomeCycleDbContext _db;

        public AgreementFormRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<agreement_form?> GetByNegotiationIdAsync(Guid negotiationId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Agreement_Forms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NegotiationId == negotiationId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<agreement_form?> GetByIdAsync(Guid agreementId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Agreement_Forms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AgreementId == agreementId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(agreement_form agreement, CancellationToken cancellationToken = default)
        {
            var entity = agreement.ToInfrastructure();
            await _db.Agreement_Forms.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(agreement_form agreement, CancellationToken cancellationToken = default)
        {
            var entity = agreement.ToInfrastructure();
            _db.Agreement_Forms.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<PagedResult<PendingAgreementListItemDto>> GetPendingPaymentByBuyerAsync(
            Guid buyerId,
            PendingAgreementSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Agreement_Forms
                .AsNoTracking()
                .Where(x => x.BuyerId == buyerId && x.AgreementStatus == (int)AgreementStatus.Awaiting_Payment);

            // Ưu tiên search theo tên sản phẩm (Product.ProductName), fallback về Post.Description
            // vì Product là quan hệ 0..1 với Post (có thể null với 1 số loại bài đăng).
            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(x =>
                    (x.Post.Product != null && x.Post.Product.ProductName != null && x.Post.Product.ProductName.Contains(request.Keyword))
                    || (x.Post.Description != null && x.Post.Description.Contains(request.Keyword)));

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new PendingAgreementListItemDto
                {
                    AgreementId = x.AgreementId,
                    ProductName = x.Post.Product != null ? x.Post.Product.ProductName : x.Post.Description,
                    ThumbnailUrl = _db.Media
                        .Where(m => m.TargetId == x.PostId && m.TargetType == "Post")
                        .OrderBy(m => m.DisplayOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault(),
                    Quantity = x.Quantity,
                    FinalPrice = x.FinalPrice,
                    InitialPrice = x.InitialPrice,
                    SellerName = x.Seller.Username,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<PendingAgreementListItemDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

    }
}
