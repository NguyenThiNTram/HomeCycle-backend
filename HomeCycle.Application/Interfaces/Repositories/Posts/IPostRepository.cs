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
        Task AddAsync(post entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(post entity, CancellationToken cancellationToken = default);

        Task<post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<post?> GetDetailByIdAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<post>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid postId, CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusAsync(Guid postId, PostStatus status, CancellationToken cancellationToken = default);
    }
}
