using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Products
{
    public interface IProductService
    {
        Task<Result<product>> PrepareForCreateAsync(
             Guid postId, ProductRequest request, CancellationToken cancellationToken = default);

        Task<Result<product>> PrepareForUpdateAsync(
            Guid postId, ProductRequest request, CancellationToken cancellationToken = default);

        Task<Result<product>> PrepareForRequirementAsync(
            Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default);

        Task<Result<product>> UpdateForRequirementAsync(
            Guid postId, ProductRequirementRequest request, CancellationToken cancellationToken = default);

        Task<Result<ProductResponse>> GetDetailByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> GetDetailAsync(Guid productId, CancellationToken cancellationToken = default);
    }

}
