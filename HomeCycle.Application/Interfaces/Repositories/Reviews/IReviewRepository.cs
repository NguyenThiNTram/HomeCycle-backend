using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Responses.Reviews;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Reviews
{
    public interface IReviewRepository
    {
        Task<review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default);

        Task<review?> GetByOrderAndReviewerAsync(Guid orderId, Guid reviewerId, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid orderId, Guid reviewerId, CancellationToken ct = default);

        Task AddAsync(review review, CancellationToken ct = default);

        Task UpdateAsync(review review, CancellationToken ct = default);

        Task<IReadOnlyList<review>> GetValidReviewsByRevieweeAsync(Guid revieweeId, CancellationToken ct = default);

        Task<PagedResult<ReviewResponseDto>> GetPagedByRevieweeAsync(Guid revieweeId, int pageNumber, int pageSize, CancellationToken ct = default);

        Task<PagedResult<ReviewResponseDto>> GetPagedByOrderAsync(Guid orderId, int pageNumber, int pageSize, CancellationToken ct = default);

        Task<(double AverageRating, int TotalReviews)> GetRatingStatsByRevieweeIdAsync(Guid revieweeId, CancellationToken cancellationToken = default);

    }
}
