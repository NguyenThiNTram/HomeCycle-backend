using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.SubscriptionPackages
{
    public interface ISubscriptionPackageService
    {
        Task<Result<bool>> DeleteAsync(Guid adminId, Guid packageId, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<SubscriptionPackageResponseDto>>> GetAllAsync(bool? isActive, UserRole? targetRole, CancellationToken cancellationToken = default);
        Task<Result<SubscriptionPackageResponseDto>> GetByIdAsync(Guid packageId, CancellationToken cancellationToken = default);
        Result<IReadOnlyList<EntitlementDefinitionResponseDto>> GetEntitlementDefinitions();
        Task<Result<SubscriptionPackageResponseDto>> CreateAsync(Guid adminId, CreateSubscriptionPackageRequest request, CancellationToken cancellationToken = default);
        Task<Result<SubscriptionPackageResponseDto>> UpdateAsync(Guid adminId, Guid packageId, UpdateSubscriptionPackageRequest request, CancellationToken cancellationToken = default);
        Task<Result<SubscriptionPackageResponseDto>> UpdateStatusAsync(Guid adminId, Guid packageId, bool isActive, CancellationToken cancellationToken = default);
    }
}
