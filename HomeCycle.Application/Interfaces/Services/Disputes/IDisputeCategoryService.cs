using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Disputes;
using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Disputes
{
    public interface IDisputeCategoryService
    {
        Task<Result<IReadOnlyList<DisputeCategoryResponseDto>>> GetAllAsync(bool? isActive, CancellationToken ct = default);
        Task<Result<IReadOnlyList<DisputeCategoryOptionDto>>> GetActiveOptionsAsync(DisputeTargetType? targetType, CancellationToken ct = default);
        Task<Result<DisputeCategoryResponseDto>> CreateAsync(Guid adminId, CreateDisputeCategoryRequest request, CancellationToken ct = default);
        Task<Result<DisputeCategoryResponseDto>> UpdateAsync(int categoryId, UpdateDisputeCategoryRequest request, CancellationToken ct = default);
        Task<Result<DisputeCategoryResponseDto>> UpdateStatusAsync(int categoryId, bool isActive, CancellationToken ct = default);
    }
}
