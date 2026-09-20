using AutoMapper;
using FluentValidation;
using HomeCycle.Application.Commons.Audits;
using HomeCycle.Application.Commons.Errors;
using HomeCycle.Application.Commons.Helpers;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.SubscriptionPackages;
using HomeCycle.Application.DTOs.Responses.SubscriptionPackages;
using HomeCycle.Application.Entitlements;
using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.SubscriptionPackages;
using HomeCycle.Application.Interfaces.Services.Audits;
using HomeCycle.Application.Interfaces.Services.SubscriptionPackages;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.SubscriptionPackages
{
    public class SubscriptionPackageService : ISubscriptionPackageService
    {
        private readonly ISubscriptionPackageRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSubscriptionPackageRequest> _createValidator;
        private readonly IValidator<UpdateSubscriptionPackageRequest> _updateValidator;

        public SubscriptionPackageService(
            ISubscriptionPackageRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IMapper mapper,
            IValidator<CreateSubscriptionPackageRequest> createValidator,
            IValidator<UpdateSubscriptionPackageRequest> updateValidator)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<bool>> DeleteAsync(Guid adminId, Guid packageId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var package = await _repository.GetByIdForUpdateAsync(packageId, cancellationToken);
                if (package == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Fail(SubscriptionPackageErrors.NotFound);
                }
                if (package.TargetRole != UserRole.Business || await _repository.HasSubscriptionsAsync(packageId, cancellationToken))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Fail(ValidationErrors.InvalidRequest("Chỉ xóa gói Business chưa có đăng ký. Gói đã có lịch sử phải dùng chức năng đóng gói."));
                }
                await _repository.DeleteAsync(packageId, cancellationToken);
                await _auditService.EnqueueAsync(new AuditEvent
                {
                    Category = AuditCategory.Administration, Action = AuditActions.SubscriptionPackageDelete,
                    Outcome = AuditOutcome.Success, ActorType = AuditActorType.User, UserId = adminId,
                    TargetType = AuditTargetTypes.SubscriptionPackage, TargetId = packageId,
                    OldValues = new Dictionary<string, object?> { ["code"] = package.Code, ["name"] = package.Name, ["price"] = package.Price }
                }, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<Result<IReadOnlyList<SubscriptionPackageResponseDto>>> GetAllAsync(
            bool? isActive,
            UserRole? targetRole,
            CancellationToken cancellationToken = default)
        {
            if (targetRole.HasValue &&
                targetRole.Value is not UserRole.Personal and not UserRole.Business)
            {
                return Result<IReadOnlyList<SubscriptionPackageResponseDto>>.Fail(
                    ValidationErrors.InvalidRequest("TargetRole is invalid."));
            }

            var packages = await _repository.GetAllAsync(
                isActive,
                targetRole,
                cancellationToken);

            var response = packages
                .Select(x => _mapper.Map<SubscriptionPackageResponseDto>(x))
                .ToList();

            return Result<IReadOnlyList<SubscriptionPackageResponseDto>>.Success(response);
        }

        public async Task<Result<SubscriptionPackageResponseDto>> GetByIdAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            var package = await _repository.GetByIdAsync(packageId, cancellationToken);

            if (package == null)
                return Result<SubscriptionPackageResponseDto>.Fail(SubscriptionPackageErrors.NotFound);

            return Result<SubscriptionPackageResponseDto>.Success(
                _mapper.Map<SubscriptionPackageResponseDto>(package));
        }

        public Result<IReadOnlyList<EntitlementDefinitionResponseDto>> GetEntitlementDefinitions()
        {
            var response = EntitlementCatalog.All
                .Select(x => _mapper.Map<EntitlementDefinitionResponseDto>(x))
                .ToList();

            return Result<IReadOnlyList<EntitlementDefinitionResponseDto>>.Success(response);
        }

        public async Task<Result<SubscriptionPackageResponseDto>> CreateAsync(
            Guid adminId,
            CreateSubscriptionPackageRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<SubscriptionPackageResponseDto>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));
            }

            var entitlementError = ValidateVipConfiguration(request.Duration, request.Entitlements, request.TargetRole);
            if (entitlementError != null)
                return Result<SubscriptionPackageResponseDto>.Fail(entitlementError);

            var code = request.Code.Trim().ToUpperInvariant();
            var name = request.Name.Trim();

            if (await _repository.ExistsByCodeAsync(code, cancellationToken))
                return Result<SubscriptionPackageResponseDto>.Fail(SubscriptionPackageErrors.CodeAlreadyExists);

            if (await _repository.ExistsByNameAsync(name, cancellationToken: cancellationToken))
                return Result<SubscriptionPackageResponseDto>.Fail(SubscriptionPackageErrors.NameAlreadyExists);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var package = _mapper.Map<subscription_package>(request);

                package.PackageId = Guid.NewGuid();
                package.Code = code;
                package.Name = name;
                package.Description = NormalizeDescription(request.Description);
                package.IsActive = true;
                package.CreatedAt = now;
                package.UpdatedAt = now;
                package.Entitlements = BuildEntitlements(request.Entitlements, package.PackageId, now);

                await _repository.AddAsync(package, cancellationToken);
                await _auditService.EnqueueAsync(
                    new AuditEvent
                    {
                        Category = AuditCategory.Administration,
                        Action = AuditActions.SubscriptionPackageCreate,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.User,
                        UserId = adminId,
                        TargetType = AuditTargetTypes.SubscriptionPackage,
                        TargetId = package.PackageId,
                        NewValues = new Dictionary<string, object?>
                        {
                            ["code"] = package.Code,
                            ["name"] = package.Name,
                            ["price"] = package.Price,
                            ["duration"] = package.Duration,
                            ["targetRole"] = package.TargetRole.ToString(),
                            ["isActive"] = package.IsActive,
                            ["entitlements"] = EntitlementSignature(package.Entitlements)
                        }
                    },
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<SubscriptionPackageResponseDto>.Success(
                    _mapper.Map<SubscriptionPackageResponseDto>(package));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<Result<SubscriptionPackageResponseDto>> UpdateAsync(
            Guid adminId,
            Guid packageId,
            UpdateSubscriptionPackageRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return Result<SubscriptionPackageResponseDto>.Fail(
                    ValidationErrors.InvalidRequest(
                        string.Join("\n", validation.Errors.Select(x => x.ErrorMessage))));
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var package = await _repository.GetByIdForUpdateAsync(packageId, cancellationToken);

                if (package == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Fail(SubscriptionPackageErrors.NotFound);
                }

                if (request.Name != null &&
                    await _repository.ExistsByNameAsync(
                        request.Name.Trim(),
                        packageId,
                        cancellationToken))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Fail(
                        SubscriptionPackageErrors.NameAlreadyExists);
                }

                var effectiveEntitlements = request.Entitlements ?? package.Entitlements.Select(x => new PackageEntitlementRequest
                {
                    Key = x.EntitlementKey,
                    NumericValue = x.NumericValue,
                    BooleanValue = x.BooleanValue,
                    IsUnlimited = x.IsUnlimited
                }).ToList();
                var configurationError = ValidateVipConfiguration(request.Duration ?? package.Duration, effectiveEntitlements, package.TargetRole);
                if (configurationError != null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Fail(configurationError);
                }

                var oldEntitlementSignature = EntitlementSignature(package.Entitlements);
                var oldName = package.Name;
                var oldDescription = package.Description;
                var oldPrice = package.Price;
                var oldDuration = package.Duration;

                _mapper.Map(request, package);

                if (request.Name != null)
                    package.Name = request.Name.Trim();

                if (request.Description != null)
                    package.Description = NormalizeDescription(request.Description);

                var now = DateTime.UtcNow;

                if (request.Entitlements != null)
                {
                    package.Entitlements = BuildEntitlements(
                        request.Entitlements,
                        package.PackageId,
                        now);
                }

                var newEntitlementSignature = EntitlementSignature(package.Entitlements);
                var diff = new AuditDiffBuilder()
                    .Add("name", oldName, package.Name)
                    .Add("description", oldDescription, package.Description)
                    .Add("price", oldPrice, package.Price)
                    .Add("duration", oldDuration, package.Duration)
                    .Add("entitlements", oldEntitlementSignature, newEntitlementSignature);

                if (diff.NewValues == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Success(
                        _mapper.Map<SubscriptionPackageResponseDto>(package));
                }

                package.UpdatedAt = now;

                await _repository.UpdateAsync(package, cancellationToken);

                if (request.Entitlements != null)
                {
                    await _repository.ReplaceEntitlementsAsync(
                        package.PackageId,
                        package.Entitlements,
                        cancellationToken);
                }

                await _auditService.EnqueueAsync(
                    new AuditEvent
                    {
                        Category = AuditCategory.Administration,
                        Action = AuditActions.SubscriptionPackageUpdate,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.User,
                        UserId = adminId,
                        TargetType = AuditTargetTypes.SubscriptionPackage,
                        TargetId = package.PackageId,
                        OldValues = diff.OldValues,
                        NewValues = diff.NewValues
                    },
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<SubscriptionPackageResponseDto>.Success(
                    _mapper.Map<SubscriptionPackageResponseDto>(package));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<Result<SubscriptionPackageResponseDto>> UpdateStatusAsync(
            Guid adminId,
            Guid packageId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var package = await _repository.GetByIdForUpdateAsync(packageId, cancellationToken);

                if (package == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Fail(SubscriptionPackageErrors.NotFound);
                }

                if (package.IsActive == isActive)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<SubscriptionPackageResponseDto>.Success(
                        _mapper.Map<SubscriptionPackageResponseDto>(package));
                }

                var previousStatus = package.IsActive;
                package.IsActive = isActive;
                package.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(package, cancellationToken);
                await _auditService.EnqueueAsync(
                    new AuditEvent
                    {
                        Category = AuditCategory.Administration,
                        Action = AuditActions.SubscriptionPackageUpdateStatus,
                        Outcome = AuditOutcome.Success,
                        ActorType = AuditActorType.User,
                        UserId = adminId,
                        TargetType = AuditTargetTypes.SubscriptionPackage,
                        TargetId = package.PackageId,
                        OldValues = new Dictionary<string, object?>
                        {
                            ["isActive"] = previousStatus
                        },
                        NewValues = new Dictionary<string, object?>
                        {
                            ["isActive"] = package.IsActive
                        }
                    },
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<SubscriptionPackageResponseDto>.Success(
                    _mapper.Map<SubscriptionPackageResponseDto>(package));
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }

        internal static Error? ValidateVipConfiguration(int duration, IReadOnlyCollection<PackageEntitlementRequest> entitlements, UserRole role)
        {
            if (role == UserRole.Business)
            {
                if (duration is < 1 or > 3650)
                    return SubscriptionPackageErrors.InvalidEntitlement("Thời hạn gói Business phải từ 1 đến 3650 ngày.");
                if (entitlements.Count == 0 || entitlements.Any(x => string.IsNullOrWhiteSpace(x.Key))
                    || entitlements.Select(x => x.Key.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entitlements.Count)
                    return SubscriptionPackageErrors.InvalidEntitlement("Quyền lợi phải có ít nhất một mục và không trùng key.");
                return ValidateEntitlements(entitlements, role);
            }
            if (duration != 30 || role != UserRole.Personal)
                return SubscriptionPackageErrors.InvalidEntitlement("Gói Personal VIP phải có thời hạn 30 ngày.");

            var error = ValidateEntitlements(entitlements, role);
            if (error != null)
                return error;

            if (entitlements.Count != 1)
                return SubscriptionPackageErrors.InvalidEntitlement("Bộ quyền lợi VIP không đúng với role của gói.");

            var ai = entitlements.SingleOrDefault(x => string.Equals(x.Key.Trim(), EntitlementKeys.PriceSuggestionDailyCount, StringComparison.OrdinalIgnoreCase));
            if (ai == null || ai.IsUnlimited || ai.NumericValue != 50)
                return SubscriptionPackageErrors.InvalidEntitlement("Personal VIP cần 50 lượt AI giá.");

            return null;
        }

        private static Error? ValidateEntitlements(
            IReadOnlyCollection<PackageEntitlementRequest> entitlements,
            UserRole targetRole)
        {
            foreach (var entitlement in entitlements)
            {
                var key = entitlement.Key.Trim().ToLowerInvariant();

                if (!EntitlementCatalog.TryGet(key, out var definition) || definition == null)
                {
                    return SubscriptionPackageErrors.InvalidEntitlement(
                        $"Entitlement '{key}' is not supported.");
                }

                if (!definition.TargetRoles.Contains(targetRole))
                {
                    return SubscriptionPackageErrors.InvalidEntitlement(
                        $"Entitlement '{key}' does not support target role '{targetRole}'.");
                }

                if (definition.ValueType == EntitlementValueType.Boolean)
                {
                    if (entitlement.IsUnlimited ||
                        entitlement.NumericValue.HasValue ||
                        !entitlement.BooleanValue.HasValue)
                    {
                        return SubscriptionPackageErrors.InvalidEntitlement(
                            $"Entitlement '{key}' requires a boolean value.");
                    }

                    continue;
                }

                if (entitlement.BooleanValue.HasValue)
                {
                    return SubscriptionPackageErrors.InvalidEntitlement(
                        $"Entitlement '{key}' does not support a boolean value.");
                }

                if (entitlement.IsUnlimited)
                {
                    if (!definition.SupportsUnlimited)
                    {
                        return SubscriptionPackageErrors.InvalidEntitlement(
                            $"Entitlement '{key}' does not support unlimited value.");
                    }

                    if (entitlement.NumericValue.HasValue)
                    {
                        return SubscriptionPackageErrors.InvalidEntitlement(
                            $"Entitlement '{key}' cannot contain NumericValue when IsUnlimited is true.");
                    }

                    continue;
                }

                if (!entitlement.NumericValue.HasValue || entitlement.NumericValue.Value <= 0)
                {
                    return SubscriptionPackageErrors.InvalidEntitlement(
                        $"Entitlement '{key}' requires a NumericValue greater than zero.");
                }

                if (definition.ValueType == EntitlementValueType.Integer &&
                    (decimal.Truncate(entitlement.NumericValue.Value) != entitlement.NumericValue.Value
                        || entitlement.NumericValue.Value > int.MaxValue))
                {
                    return SubscriptionPackageErrors.InvalidEntitlement(
                        $"Entitlement '{key}' requires an integer value.");
                }
            }

            return null;
        }

        private List<subscription_package_entitlement> BuildEntitlements(
            IEnumerable<PackageEntitlementRequest> requests,
            Guid packageId,
            DateTime now)
        {
            return requests.Select(request =>
            {
                var key = request.Key.Trim().ToLowerInvariant();
                EntitlementCatalog.TryGet(key, out var definition);

                var entitlement = _mapper.Map<subscription_package_entitlement>(request);
                entitlement.PackageEntitlementId = Guid.NewGuid();
                entitlement.PackageId = packageId;
                entitlement.EntitlementKey = key;
                entitlement.ValueType = definition!.ValueType;
                entitlement.CreatedAt = now;
                entitlement.UpdatedAt = now;

                return entitlement;
            }).ToList();
        }

        private static string EntitlementSignature(
            IEnumerable<subscription_package_entitlement> entitlements)
        {
            return string.Join(
                "|",
                entitlements
                    .OrderBy(x => x.EntitlementKey)
                    .Select(x =>
                        $"{x.EntitlementKey}:{x.ValueType}:" +
                        $"{x.NumericValue?.ToString(CultureInfo.InvariantCulture) ?? "null"}:" +
                        $"{x.BooleanValue?.ToString() ?? "null"}:{x.IsUnlimited}"));
        }

        private static string? NormalizeDescription(string? description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();
        }
    }
}
