using HomeCycle.Application.DTOs.Requests.Offers;
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
    public partial class OfferRepository : IOfferRepository
    {
        private readonly HomeCycleDbContext _db;

        private readonly HomeCycle.Application.Interfaces.Generics.IUnitOfWork _unit;
        private readonly HomeCycle.Application.Interfaces.Repositories.Offers.IChatRealtimePublisher _publisher;
        private readonly AutoMapper.IMapper _mapper;
        public OfferRepository(HomeCycleDbContext db, HomeCycle.Application.Interfaces.Generics.IUnitOfWork unit,
            HomeCycle.Application.Interfaces.Repositories.Offers.IChatRealtimePublisher publisher, AutoMapper.IMapper mapper)
        {
            _db = db; _unit = unit; _publisher = publisher; _mapper = mapper;
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
                .Include(x => x.Post).ThenInclude(p => p.Product)
                .Include(x => x.BuyPost).ThenInclude(p => p!.Product)
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .FirstOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<offer>> GetSentAsync(Guid senderId, OfferSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Offers
                .AsNoTracking()
                .Include(o => o.Sender)
                .Include(o => o.Receiver)
                .Include(x => x.Post)
                    .ThenInclude(x => x!.Product)
                .Where(x => x.SenderId == senderId);

            if (request.PostId.HasValue) query = query.Where(x => x.PostId == request.PostId);
            if (request.BuyPostId.HasValue) query = query.Where(x => x.BuyPostId == request.BuyPostId);
            if (request.Status.HasValue) query = query.Where(x => x.OfferStatus == (int)request.Status.Value);
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

        public async Task<PagedResult<offer>> GetReceivedAsync(Guid receiverId, OfferSearchRequest request, CancellationToken cancellationToken = default)
        {
            var query = _db.Offers
                .AsNoTracking()
                .Include(o => o.Sender)
                .Include(o => o.Receiver)
                .Include(x => x.Post)
                    .ThenInclude(x => x!.Product)
                .Where(x => x.ReceiverId == receiverId);

            if (request.PostId.HasValue) query = query.Where(x => x.PostId == request.PostId);
            if (request.BuyPostId.HasValue) query = query.Where(x => x.BuyPostId == request.BuyPostId);
            if (request.Status.HasValue) query = query.Where(x => x.OfferStatus == (int)request.Status.Value);
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

        public async Task<bool> ExistsPendingByPostAndSenderAsync(Guid postId, Guid senderId, Guid receiverId, Guid? buyPostId, CancellationToken cancellationToken = default)
        {
            return await _db.Offers.AnyAsync(
                x => x.PostId == postId
                  && x.SenderId == senderId && x.ReceiverId == receiverId && x.BuyPostId == buyPostId
                  && x.OfferStatus == (int)HomeCycle.Domain.Enums.OfferStatus.Pending,
                cancellationToken);
        }


    }
}
