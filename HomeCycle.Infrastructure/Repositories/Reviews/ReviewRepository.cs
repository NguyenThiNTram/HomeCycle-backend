using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Responses.Reviews;
using HomeCycle.Application.Interfaces.Repositories.Reviews;
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

namespace HomeCycle.Infrastructure.Repositories.Reviews
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly HomeCycleDbContext _db;

        public ReviewRepository(HomeCycleDbContext db) => _db = db;

        public async Task<review?> GetByIdForUpdateAsync(Guid reviewId, CancellationToken ct = default)
        {
            var entity = await _db.Reviews
                .FromSqlInterpolated($"SELECT * FROM public.\"Review\" WHERE \"ReviewId\" = {reviewId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(ct);
            return entity?.ToDomain();
        }

        public async Task<bool> TryUpdateVisibleAsync(review review, CancellationToken ct = default)
        {
            // Recheck visibility in SQL so an edit racing moderation cannot unhide the review.
            var affected = await _db.Reviews.Where(x => x.ReviewId == review.ReviewId &&
                    (x.ReviewStatus == (int)ReviewStatus.Active || x.ReviewStatus == (int)ReviewStatus.Edited))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Rating, review.Rating)
                    .SetProperty(x => x.Comment, review.Comment)
                    .SetProperty(x => x.ReviewStatus, review.ReviewStatus)
                    .SetProperty(x => x.UpdatedAt, review.UpdatedAt), ct);
            return affected == 1;
        }

        public async Task<review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default)
        {
            var entity = await _db.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReviewId == reviewId, ct);

            return entity?.ToDomain();
        }

        public async Task<review?> GetByOrderAndReviewerAsync(Guid orderId, Guid reviewerId, CancellationToken ct = default)
        {
            var entity = await _db.Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.ReviewerId == reviewerId, ct);

            return entity?.ToDomain();
        }

        public async Task<bool> ExistsAsync(Guid orderId, Guid reviewerId, CancellationToken ct = default)
        {
            return await _db.Reviews
                .AsNoTracking()
                .AnyAsync(x => x.OrderId == orderId && x.ReviewerId == reviewerId, ct);
        }

        public async Task AddAsync(review review, CancellationToken ct = default)
        {
            await _db.Reviews.AddAsync(review.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(review review, CancellationToken ct = default)
        {
            var entity = review.ToInfrastructure();
            var localEntry = _db.Reviews.Local.FirstOrDefault(x => x.ReviewId == entity.ReviewId);

            if (localEntry != null)
                _db.Entry(localEntry).State = EntityState.Detached;

            _db.Reviews.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<review>> GetValidReviewsByRevieweeAsync(Guid revieweeId, CancellationToken ct = default)
        {
            var entities = await _db.Reviews
                .AsNoTracking()
                .Where(x => x.RevieweeId == revieweeId)
                .Where(x => x.ReviewStatus == (int)ReviewStatus.Active || x.ReviewStatus == (int)ReviewStatus.Edited)
                .ToListAsync(ct);

            return entities.Select(x => x.ToDomain()).ToList();
        }

        public async Task<PagedResult<ReviewResponseDto>> GetPagedByRevieweeAsync(
            Guid revieweeId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Reviews
                .AsNoTracking()
                .Where(x => x.RevieweeId == revieweeId &&
                    (x.ReviewStatus == (int)ReviewStatus.Active || x.ReviewStatus == (int)ReviewStatus.Edited));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReviewResponseDto
                {
                    ReviewId = x.ReviewId,
                    OrderId = x.OrderId,
                    ReviewerId = x.ReviewerId,
                    RevieweeId = x.RevieweeId,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    ReviewStatus = x.ReviewStatus,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    ReviewerName = x.Reviewer.Username,
                    ReviewerAvatarUrl = x.Reviewer.AvatarUrl,
                    RevieweeName = x.Reviewee.Username,
                    RevieweeAvatarUrl = x.Reviewee.AvatarUrl
                })
                .ToListAsync(ct);

            return new PagedResult<ReviewResponseDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<ReviewResponseDto>> GetPagedByOrderAsync(
            Guid orderId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _db.Reviews
                .AsNoTracking()
                .Where(x => x.OrderId == orderId &&
                    (x.ReviewStatus == (int)ReviewStatus.Active || x.ReviewStatus == (int)ReviewStatus.Edited));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReviewResponseDto
                {
                    ReviewId = x.ReviewId,
                    OrderId = x.OrderId,
                    ReviewerId = x.ReviewerId,
                    RevieweeId = x.RevieweeId,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    ReviewStatus = x.ReviewStatus,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    ReviewerName = x.Reviewer.Username,
                    ReviewerAvatarUrl = x.Reviewer.AvatarUrl,
                    RevieweeName = x.Reviewee.Username,
                    RevieweeAvatarUrl = x.Reviewee.AvatarUrl
                })
                .ToListAsync(ct);

            return new PagedResult<ReviewResponseDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<(double AverageRating, int TotalReviews)> GetRatingStatsByRevieweeIdAsync(Guid revieweeId, CancellationToken cancellationToken = default)
        {
            var query = _db.Reviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == revieweeId && r.Rating.HasValue &&
                    (r.ReviewStatus == (int)ReviewStatus.Active || r.ReviewStatus == (int)ReviewStatus.Edited));

            var totalReviews = await query.CountAsync(cancellationToken);

            if (totalReviews == 0)
                return (0, 0);

            var averageRating = await query.AverageAsync(r => r.Rating!.Value, cancellationToken);

            return (Math.Round(averageRating, 1), totalReviews);
        }
    }
}
