using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Carts;
using HomeCycle.Application.DTOs.Responses.Carts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Carts
{
    public interface ICartService
    {
        Task<Result<CartResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<CartItemResponse>> AddAsync(Guid userId, Guid postId, AddToCartRequest request, CancellationToken cancellationToken = default);

        Task<Result<bool>> RemoveAsync(Guid userId, Guid cartItemId, CancellationToken cancellationToken = default);
    }
}
