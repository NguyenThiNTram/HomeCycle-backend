using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Responses.Posts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Posts
{
    public interface IPostService
    {
        //Task<Result<PostResponse>> CreateAsync(Guid ownerId, CreatePostRequest request, CancellationToken cancellationToken = default);

        //Task<Result<PostResponse>> UpdateAsync(Guid ownerId, Guid postId, UpdatePostRequest request, CancellationToken cancellationToken = default);

        //Task<Result<PostDetailResponse>> GetDetailAsync(Guid postId, CancellationToken cancellationToken = default);

        //Task<Result<PagedResult<PostResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

        //Task<Result<PagedResult<PostResponse>>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default);

        //Task<Result<bool>> CloseAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default);

        //Task<Result<bool>> DeleteAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default);

        Task<Result<PostResponse>> CreateSellPostAsync(Guid ownerId, CreateSellPostRequest request, CancellationToken cancellationToken = default);

        Task<Result<PostResponse>> CreateBuyPostAsync(Guid ownerId, CreateBuyPostRequest request, CancellationToken cancellationToken = default);

        Task<Result<PostResponse>> UpdateSellPostAsync(Guid ownerId, Guid postId, UpdateSellPostRequest request, CancellationToken cancellationToken = default);

        Task<Result<PostResponse>> UpdateBuyPostAsync(Guid ownerId, Guid postId, UpdateBuyPostRequest request, CancellationToken cancellationToken = default);

        Task<Result<PostDetailResponse>> GetDetailAsync(Guid postId, CancellationToken cancellationToken = default);
        Task<Result<PagedResult<PostResponse>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);
        Task<Result<PagedResult<PostResponse>>> SearchAsync(PostSearchRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> CloseAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(Guid ownerId, Guid postId, CancellationToken cancellationToken = default);
    }
}
