using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.SubscriptionPackages
{
    public sealed record SubscriptionPurchaseContext(user User, subscription_package Package);

    public interface IUserSubscriptionService
    {
        Task<Result<SubscriptionPurchaseContext>> ValidatePurchaseEligibilityAsync(Guid userId, Guid packageId, DateTime nowUtc, CancellationToken cancellationToken = default);
        Task<user_subscription> CreatePendingSubscriptionAsync(Guid userId, subscription_package package, DateTime createdAtUtc, CancellationToken cancellationToken = default);
        Task<Result<bool>> CancelActiveSubscriptionAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
        Task<Result<user_subscription>> ActivateSubscriptionAsync(Guid subscriptionId, decimal pricePaid, DateTime paidAtUtc, AuditActorType actorType, AuditSource auditSource, Guid? actorUserId, CancellationToken cancellationToken = default);
        Task<Result<bool>> CancelPendingSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
        Task<Result<UserSubscriptionResponseDto?>> GetCurrentAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    }
}
