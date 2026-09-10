using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Posts
{
    public interface IPostRepository
    {
        Task<offer?> GetTradeByAgreementAsync(Guid agreementId, CancellationToken cancellationToken = default);
        Task<offer?> GetTradeByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task RestoreOrderQuantityAsync(Guid orderId, bool restoreSellStock, CancellationToken cancellationToken = default);
        Task<int> GetReservedQuantityAsync(Guid postId, Guid? excludedNegotiationId = null, CancellationToken cancellationToken = default);
        Task<bool> HasUnfinishedTransactionsAsync(Guid postId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetExpiredBuyPostIdsAsync(DateTime now, int count, CancellationToken cancellationToken = default);
        Task<PagedResult<post>> GetMatchesAsync(post buyPost, PaginationRequest request, CancellationToken cancellationToken = default);
        Task AddAsync(post entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(post entity, CancellationToken cancellationToken = default);

        Task<post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<post?> GetByIdForUpdateAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<post?> GetDetailByIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> GetAllActiveAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> GetAllByOwnerAsync(Guid ownerId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<post?> GetDetailByOwnerAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default);

        Task<int> CountActiveByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusAsync(Guid postId, PostStatus status, CancellationToken cancellationToken = default);
    }
}
