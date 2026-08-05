using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Interfaces.Repositories.Offers;
using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Offers
{
    public class OfferRepository : IOfferRepository
    {
        private readonly HomeCycleDbContext _db;

        public OfferRepository(HomeCycleDbContext db)
        {
            _db = db;
        }

        public async Task<offer?> GetByIdForUpdateAsync(Guid offerId, CancellationToken cancellationToken)
        {
            //FOR UPDATE để khóa dòng dữ liệu trong Postgres.
            //AsNoTracking: tránh xung đột ChangeTracker với UpdateAsync (vốn tạo instance mới qua ToInfrastructure()).
            var entity = await _db.Offers
                .FromSqlInterpolated($@"
            SELECT * 
            FROM ""Offer"" 
            WHERE ""OfferId"" = {offerId} 
            FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.ToDomain();
        }

        public async Task AddAsync(offer entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            await _db.Offers.AddAsync(infraEntity, cancellationToken);
        }

        public Task UpdateAsync(offer entity, CancellationToken cancellationToken = default)
        {
            var infraEntity = entity.ToInfrastructure();
            _db.Offers.Update(infraEntity);
            return Task.CompletedTask;
        }

        public async Task<offer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Offers
                .AsNoTracking()
                .Include(x => x.Post)
                .FirstOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<offer>> GetSentAsync(Guid senderId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Offers
                .AsNoTracking()
                .Include(x => x.Post)
                .Where(x => x.SenderId == senderId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<offer>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<offer>> GetReceivedAsync(Guid receiverId, PaginationRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Offers
                .AsNoTracking()
                .Include(x => x.Post)
                .Where(x => x.ReceiverId == receiverId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<offer>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> ExistsPendingByPostAndSenderAsync(Guid postId, Guid senderId, CancellationToken cancellationToken = default)
        {
            return await _db.Offers.AnyAsync(
                x => x.PostId == postId
                  && x.SenderId == senderId
                  && x.OfferStatus == (int)HomeCycle.Domain.Enums.OfferStatus.Pending,
                cancellationToken);
        }


    }
}
