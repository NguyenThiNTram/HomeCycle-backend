using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Reviews;
using HomeCycle.Application.DTOs.Responses.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Reviews
{
    public interface IReviewService
    {
        Task<Result<ReviewResponseDto>> CreateReviewAsync(
            Guid orderId, CreateReviewRequest request, Guid currentUserId, CancellationToken ct = default);

        Task<Result<ReviewResponseDto>> UpdateReviewAsync(
            Guid reviewId, UpdateReviewRequest request, Guid currentUserId, CancellationToken ct = default);

        Task<Result<ReviewResponseDto>> GetByIdAsync(Guid reviewId, CancellationToken ct = default);

        Task<Result<ReviewResponseDto>> GetMyReviewForOrderAsync(
            Guid orderId, Guid currentUserId, CancellationToken ct = default);

        Task<Result<PagedResult<ReviewResponseDto>>> GetReviewsByUserAsync(
            Guid userId, int pageNumber, int pageSize, CancellationToken ct = default);

        Task<Result<PagedResult<ReviewResponseDto>>> GetReviewsByOrderAsync(
            Guid orderId, Guid currentUserId, int pageNumber, int pageSize, CancellationToken ct = default);
    }
}
